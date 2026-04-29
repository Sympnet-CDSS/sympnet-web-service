using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Models;

namespace SympNet.WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Patient> Patirnts => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId);
        
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);
        
        modelBuilder.Entity<Appointment>()
             .HasOne(a => a.Doctor)
             .WithMany()
             .HasForeignKey(a => a.DoctorId);
    }
}