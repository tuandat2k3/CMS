using Microsoft.EntityFrameworkCore;
using CMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Corporation> Corporations { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Models.File> Files { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PermissionCompany> PermissionCompanies { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ContractRejection> ContractRejections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Corporation relationships
            modelBuilder.Entity<Corporation>()
                .HasMany(c => c.Companies)
                .WithOne(c => c.Corporation)
                .HasForeignKey(c => c.CorporationID);

            modelBuilder.Entity<Corporation>()
                .HasMany(c => c.Contracts)
                .WithOne(c => c.Corporation)
                .HasForeignKey(c => c.CorporationID);

            modelBuilder.Entity<Corporation>()
                .HasMany(c => c.Users)
                .WithOne(u => u.Corporation)
                .HasForeignKey(u => u.CorporationID);

            modelBuilder.Entity<Corporation>()
                .HasMany(c => c.Files)
                .WithOne(f => f.Corporation)
                .HasForeignKey(f => f.CorporationID);

            // Company relationships
            modelBuilder.Entity<Company>()
                .HasMany(c => c.Branches)
                .WithOne(b => b.Company)
                .HasForeignKey(b => b.CompanyID);

            modelBuilder.Entity<Company>()
                .HasMany(c => c.Contracts)
                .WithOne(c => c.Company)
                .HasForeignKey(c => c.CompanyID);

            modelBuilder.Entity<Company>()
                .HasMany(c => c.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyID);

            modelBuilder.Entity<Company>()
                .HasMany(c => c.Files)
                .WithOne(f => f.Company)
                .HasForeignKey(f => f.CompanyID);

            modelBuilder.Entity<Company>()
                .HasMany(c => c.PermissionCompanies)
                .WithOne(p => p.Company)
                .HasForeignKey(p => p.CompanyID);

            // Branch relationships
            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Departments)
                .WithOne(d => d.Branch)
                .HasForeignKey(d => d.BranchID);

            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Contracts)
                .WithOne(c => c.Branch)
                .HasForeignKey(c => c.BranchID);

            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Users)
                .WithOne(u => u.Branch)
                .HasForeignKey(u => u.BranchID);

            modelBuilder.Entity<Branch>()
                .HasMany(b => b.Files)
                .WithOne(f => f.Branch)
                .HasForeignKey(f => f.BranchID);

            modelBuilder.Entity<Branch>()
                .HasMany(b => b.PermissionCompanies)
                .WithOne(p => p.Branch)
                .HasForeignKey(p => p.BranchID);

            // Department relationships
            modelBuilder.Entity<Department>()
                .HasMany(d => d.Positions)
                .WithOne(p => p.Department)
                .HasForeignKey(p => p.DepartmentID);

            modelBuilder.Entity<Department>()
                .HasMany(d => d.Contracts)
                .WithOne(c => c.Department)
                .HasForeignKey(c => c.DepartmentID);

            modelBuilder.Entity<Department>()
                .HasMany(d => d.Users)
                .WithOne(u => u.Department)
                .HasForeignKey(u => u.DepartmentID);

            modelBuilder.Entity<Department>()
                .HasMany(d => d.Files)
                .WithOne(f => f.Department)
                .HasForeignKey(f => f.DepartmentID);

            modelBuilder.Entity<Department>()
                .HasMany(d => d.PermissionCompanies)
                .WithOne(p => p.Department)
                .HasForeignKey(p => p.DepartmentID);

            // Position relationships
            modelBuilder.Entity<Position>()
                .HasMany(p => p.Contracts)
                .WithOne(c => c.Position)
                .HasForeignKey(p => p.PositionID);

            modelBuilder.Entity<Position>()
                .HasMany(p => p.Users)
                .WithOne(u => u.Position)
                .HasForeignKey(u => u.PositionID);

            modelBuilder.Entity<Position>()
                .HasMany(p => p.Files)
                .WithOne(f => f.Position)
                .HasForeignKey(f => f.PositionID);

            modelBuilder.Entity<Position>()
                .HasMany(p => p.PermissionCompanies)
                .WithOne(p => p.Position)
                .HasForeignKey(p => p.PositionID);

            // Contract relationships
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Partner)
                .WithMany(p => p.Contracts)
                .HasForeignKey(c => c.PartnerID);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.File)
                .WithMany(f => f.Contracts)
                .HasForeignKey(c => c.FilesAutoID);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Invoice)
                .WithMany(i => i.Contracts)
                .HasForeignKey(c => c.InvoicesAutoID);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Assignment)
                .WithMany(a => a.Contracts)
                .HasForeignKey(c => c.AssignmentsAutoID);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.Assignments)
                .WithOne(a => a.Contract)
                .HasForeignKey(a => a.ContractID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.Files)
                .WithOne(f => f.Contract)
                .HasForeignKey(f => f.ContractID);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Contract)
                .HasForeignKey(i => i.ContractID);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.Files)
                .WithOne(f => f.Contract)
                .HasForeignKey(f => f.ContractID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Contract)
                .HasForeignKey(i => i.ContractID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.Assignments)
                .WithOne(a => a.Contract)
                .HasForeignKey(a => a.ContractID)
                .OnDelete(DeleteBehavior.Cascade);

            // ContractRejection relationships
            modelBuilder.Entity<ContractRejection>()
                .HasOne(cr => cr.Contract)
                .WithMany(c => c.ContractRejections)
                .HasForeignKey(cr => cr.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // Assignment relationships
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.User)
                .WithMany(u => u.Assignments)
                .HasForeignKey(a => a.UserID);

            // File relationships
            modelBuilder.Entity<Models.File>()
                .HasOne(f => f.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(f => f.FolderID);

            modelBuilder.Entity<Models.File>()
                .HasOne(f => f.Invoice)
                .WithMany(i => i.Files)
                .HasForeignKey(f => f.InvoiceID);

            // Category relationships
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentID);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Permissions)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CateID)
                .OnDelete(DeleteBehavior.Cascade);

            // Permission relationships
            modelBuilder.Entity<Permission>()
                .HasOne(p => p.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(p => p.RoleID)
                .OnDelete(DeleteBehavior.Cascade);

            // PermissionCompany relationships
            modelBuilder.Entity<PermissionCompany>()
                .HasOne(p => p.Role)
                .WithMany(r => r.PermissionCompanies)
                .HasForeignKey(p => p.RoleID)
                .OnDelete(DeleteBehavior.Cascade);

            // User relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserRole relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RolesID)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<Branch>()
                .HasIndex(b => b.CompanyID);

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.CorporationID);

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.BranchID);

            modelBuilder.Entity<Position>()
                .HasIndex(p => p.DepartmentID);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => a.ContractID);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => a.UserID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.AssignmentsAutoID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.BranchID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.CompanyID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.CorporationID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.DepartmentID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.FilesAutoID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.InvoicesAutoID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.PartnerID);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.PositionID);

            modelBuilder.Entity<Models.File>()
                .HasIndex(f => f.ContractID);

            modelBuilder.Entity<Models.File>()
                .HasIndex(f => f.FolderID);

            modelBuilder.Entity<Models.File>()
                .HasIndex(f => f.InvoiceID);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.ContractID);

            modelBuilder.Entity<PermissionCompany>()
                .HasIndex(p => p.RoleID);

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.CateID);

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.RoleID);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.BranchID);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.CompanyID);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.CorporationID);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.DepartmentID);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PositionID);

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.RolesID);

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserID);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreateDate);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.UserID, a.CreateDate });

            base.OnModelCreating(modelBuilder);
        }
    }
}