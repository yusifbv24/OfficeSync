using FileService.Domain.Common;
using FileService.Infrastructure.Configurations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using File = FileService.Domain.Entities.File;
using FileAccess = FileService.Domain.Entities.FileAccess;

namespace FileService.Infrastructure.Data
{
    public class FileServiceDbContext:DbContext
    {
        private readonly IMediator _mediator;

        public FileServiceDbContext(
            DbContextOptions<FileServiceDbContext> options,
            IMediator mediator):base(options)
        {
            _mediator= mediator;
        }

        public DbSet<File> Files => Set<File>();

        public DbSet<FileAccess> FileAccesses => Set<FileAccess>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new FileConfiguration());
            modelBuilder.ApplyConfiguration(new FileAccessConfiguration());

            // Configure global query filter to automatically exclude soft-deleted files
            // This filter is applied to ALL queries on the Files DbSet unless explicitly
            // overridden with IgnoreQueryFilters()
            // 
            // This is extremely useful because it means you don't have to remember to
            // add ".Where(f => !f.IsDeleted)" to every query - it happens automatically
            modelBuilder.Entity<File>().HasQueryFilter(f => !f.IsDeleted);
        }

        /// <summary>
        /// Overriding SaveChangesAsync to add custom logic before/after saving.
        /// This is where we publish domain events that have been raised by entities
        /// during their lifecycle.
        /// 
        /// The process flow is:
        /// 1. EF Core saves all pending changes to the database in a transaction
        /// 2. If save is successful, we collect all domain events from modified entities
        /// 3. We publish those events so other parts of the system can react
        /// 4. We clear the events from entities to prevent re-publishing
        /// 
        /// This ensures events are only published for changes that were actually
        /// persisted, maintaining data consistency.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // First, save all changes to the database
            // This happens in a transaction, so either all changes succeed or all fail
            var result =await base.SaveChangesAsync(cancellationToken);

            // After successful save, publish domain events
            await PublishDomainEventsAsync(cancellationToken);

            return result;
        }



        /// <summary>
        /// Collects and publishes domain events from all modified entities.
        /// Domain events represent significant things that happened in the domain,
        /// like "FileUploaded" or "FileDeleted". Other parts of the system can
        /// subscribe to these events to perform related actions.
        /// 
        /// For example, when a FileDeletedEvent is published:
        /// - The Messaging Service might remove file attachment references
        /// - An audit service might log the deletion
        /// - A notification service might alert relevant users
        /// 
        /// This decouples these concerns from the core file deletion logic,
        /// making the system more maintainable and flexible.
        /// </summary>
        private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
        {
            // Get all entities that inherit from BaseEntity (which have domain events)
            var entitiesWithEvents = ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            // Collect all domain events from these entities
            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // Clear events from entities BEFORE publishing
            // This prevents events from being published multiple times
            // if SaveChanges is called again
            entitiesWithEvents.ForEach(e => e.ClearDomainEvents());


            // Publish each event through MediatR
            // MediatR will find all handlers registered for each event type
            // and execute them. This happens asynchronously but sequentially
            // to ensure proper ordering of event handling
            foreach(var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }
    }
}