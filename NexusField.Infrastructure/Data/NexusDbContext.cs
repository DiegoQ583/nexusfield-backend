using Microsoft.EntityFrameworkCore;
using NexusField.Application.Interfaces;
using NexusField.Domain.Entities;
using NexusField.Domain.Enums;
using NexusField.Domain.Interfaces;

namespace NexusField.Infrastructure.Data;

public class NexusDbContext(
    DbContextOptions<NexusDbContext> options,
    ICurrentUserService currentUserService) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<Repuesto> Repuestos => Set<Repuesto>();
    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();
    public DbSet<OrdenRepuesto> OrdenRepuestos => Set<OrdenRepuesto>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.FechaCreacion = DateTime.UtcNow;
                    entry.Entity.UsuarioModificacionId = currentUserService.UsuarioId;
                    break;
                case EntityState.Modified:
                    entry.Entity.FechaModificacion = DateTime.UtcNow;
                    entry.Entity.UsuarioModificacionId = currentUserService.UsuarioId;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── Usuario ───────────────────────────────────────────────────────
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(u => u.NombreUsuario).IsUnique();

            entity.Property(u => u.NombreCompleto).HasMaxLength(150).IsRequired();
            entity.Property(u => u.NombreUsuario).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Correo).HasMaxLength(150).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Telefono).HasMaxLength(20);
            entity.Property(u => u.Especialidad).HasMaxLength(100);
            entity.Property(u => u.TarifaPorHora).HasColumnType("decimal(10,2)");

            // Persiste el enum como texto para legibilidad y estabilidad en la BD.
            entity.Property(u => u.Rol)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Data Seeding: hash BCrypt estático pre-calculado para "Admin123!".
            // Se usa un valor fijo (no BCrypt.HashPassword en runtime) para que
            // EF Core no detecte cambios y genere migraciones infinitas.
            entity.HasData(new Usuario
            {
                Id = 1,
                NombreCompleto = "Admin Sistema",
                NombreUsuario = "admin",
                Correo = "admin@nexusfield.com",
                PasswordHash = "$2a$11$aYq3vT61/8I9eZOu2xyaXejK5vL5XGiMlRjH.CsULE1TGfhbXgYOa",
                Rol = RolUsuario.Despachador,
                Activo = true,
                TarifaPorHora = 0
            });
        });

        // ─── Activo ────────────────────────────────────────────────────────
        modelBuilder.Entity<Activo>(entity =>
        {
            entity.Property(a => a.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Ubicacion).HasMaxLength(300).IsRequired();
            entity.Property(a => a.ClienteActual).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Estado).HasMaxLength(50).IsRequired();

            // Mapea la propiedad Activo_ (con underscore para evitar colisión con el nombre de clase)
            // a la columna "Activo" en la base de datos.
            entity.Property(a => a.Activo_).HasColumnName("Activo");
        });

        // ─── Repuesto ──────────────────────────────────────────────────────
        modelBuilder.Entity<Repuesto>(entity =>
        {
            entity.Property(r => r.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Descripcion).HasMaxLength(500);
            entity.Property(r => r.Precio).HasColumnType("decimal(18,2)");
            entity.Property(r => r.FotoUrl).HasMaxLength(500);

            // Ignorar la propiedad calculada; no se persiste en BD.
            entity.Ignore(r => r.StockDisponible);
        });

        // ─── OrdenTrabajo ──────────────────────────────────────────────────
        modelBuilder.Entity<OrdenTrabajo>(entity =>
        {
            entity.Property(o => o.Titulo).HasMaxLength(200).IsRequired();
            entity.Property(o => o.Descripcion).HasMaxLength(1000).IsRequired();
            entity.Property(o => o.HorasTrabajadas).HasColumnType("decimal(10,2)");
            entity.Property(o => o.CostoTotal).HasColumnType("decimal(18,2)");
            entity.Property(o => o.NotasTecnico).HasMaxLength(2000);
            entity.Property(o => o.FotoUrl).HasMaxLength(500);
            entity.Property(o => o.ClienteHistorico).HasMaxLength(200).IsRequired();

            // Enums como texto — legibilidad en BD y resistencia a reordenamiento de valores.
            entity.Property(o => o.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(o => o.Prioridad)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            // Relación: OrdenTrabajo -> Activo (muchas órdenes por activo)
            entity.HasOne(o => o.ActivoFisico)
                .WithMany()
                .HasForeignKey(o => o.ActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación: OrdenTrabajo -> Usuario/Técnico (muchas órdenes por técnico)
            entity.HasOne(o => o.Tecnico)
                .WithMany()
                .HasForeignKey(o => o.TecnicoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── OrdenRepuesto (Tabla Puente) ──────────────────────────────────
        modelBuilder.Entity<OrdenRepuesto>(entity =>
        {
            // Llave primaria compuesta: garantiza un único registro por combinación orden-repuesto.
            entity.HasKey(or => new { or.OrdenId, or.RepuestoId });

            entity.Property(or => or.PrecioHistorico).HasColumnType("decimal(18,2)");

            entity.HasOne(or => or.Orden)
                .WithMany(o => o.RepuestosUsados)
                .HasForeignKey(or => or.OrdenId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(or => or.Repuesto)
                .WithMany()
                .HasForeignKey(or => or.RepuestoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ═══════════════════════════════════════════════════════════════════
        // DATA SEEDING — Datos de prueba con IDs fijos y fechas estáticas.
        // Se usan fechas fijas (no DateTime.UtcNow) para que EF Core no
        // detecte cambios en cada ejecución y genere migraciones infinitas.
        // ═══════════════════════════════════════════════════════════════════

        // ─── Activos de prueba ──────────────────────────────────────────
        modelBuilder.Entity<Activo>().HasData(
            new Activo
            {
                Id = 1,
                Nombre = "Ascensor Torre A",
                Ubicacion = "Sótano",
                ClienteActual = "Consorcio A",
                Estado = "Operativo",
                Activo_ = true
            },
            new Activo
            {
                Id = 2,
                Nombre = "Planta Eléctrica B",
                Ubicacion = "Azotea",
                ClienteActual = "Consorcio B",
                Estado = "Operativo",
                Activo_ = true
            });

        // ─── Repuestos de prueba ────────────────────────────────────────
        modelBuilder.Entity<Repuesto>().HasData(
            new Repuesto
            {
                Id = 1,
                Nombre = "Filtro de Aceite",
                Precio = 15.50m,
                StockTotal = 100,
                StockReservado = 0,
                Activo = true
            },
            new Repuesto
            {
                Id = 2,
                Nombre = "Botonera",
                Precio = 45.00m,
                StockTotal = 20,
                StockReservado = 0,
                Activo = true
            });

        // ─── Órdenes de trabajo de prueba ───────────────────────────────
        modelBuilder.Entity<OrdenTrabajo>().HasData(
            new OrdenTrabajo
            {
                Id = 1,
                Titulo = "Falla en puertas",
                Descripcion = "No cierran",
                ClienteHistorico = "Consorcio A",
                Estado = EstadoOrden.Pendiente,
                Prioridad = Prioridad.Alta,
                ActivoId = 1,
                TecnicoId = 1,
                FechaCreacion = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc),
                HorasTrabajadas = 0,
                CostoTotal = 0
            },
            new OrdenTrabajo
            {
                Id = 2,
                Titulo = "Mantenimiento preventivo",
                Descripcion = "Revisión mensual",
                ClienteHistorico = "Consorcio B",
                Estado = EstadoOrden.Completada,
                Prioridad = Prioridad.Media,
                ActivoId = 2,
                TecnicoId = 1,
                FechaCreacion = new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc),
                FechaCompletada = new DateTime(2026, 9, 8, 16, 0, 0, DateTimeKind.Utc),
                HorasTrabajadas = 4,
                CostoTotal = 150m
            });
    }
}
