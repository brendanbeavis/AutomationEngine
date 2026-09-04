using AutomationEngine.Data;
using AutomationEngine.Data.Entities;
using AutomationEngine.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Repository implementation for job persistence
    /// Uses IDbContextFactory for thread-safe, singleton-compatible DbContext access
    /// Single Responsibility: Database access only
    /// Does not handle caching, state transitions, or business logic
    /// </summary>
    public class JobRepository : IJobRepository
    {
        private readonly IDbContextFactory<AutomationDbContext> _contextFactory;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(IDbContextFactory<AutomationDbContext> contextFactory, ILogger<JobRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<JobEntity>> LoadAllActiveJobsAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var jobs = await context.Jobs
                    .Where(j => j.DeletedAt == null)
                    .Include(j => j.Runs
                        .Where(r => r.DeletedAt == null)
                        .OrderByDescending(r => r.StartedAt)
                        .Take(10))
                    .ToListAsync();

                StructuredLogger.LogDatabaseOperation(_logger, "LoadAllActiveJobs", "JobEntity", jobs.Count, TimeSpan.Zero);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load all active jobs from repository");
                throw;
            }
        }

        public async Task<JobEntity?> GetJobByIdAsync(string jobId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var job = await context.Jobs
                    .Where(j => j.JobId == jobId && j.DeletedAt == null)
                    .Include(j => j.Runs
                        .Where(r => r.DeletedAt == null)
                        .OrderByDescending(r => r.StartedAt)
                        .Take(10))
                    .FirstOrDefaultAsync();

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job by ID from repository | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<JobEntity?> GetJobByEntityIdAsync(int jobEntityId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var job = await context.Jobs
                    .Where(j => j.Id == jobEntityId && j.DeletedAt == null)
                    .FirstOrDefaultAsync();

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job by entity ID from repository | JobEntityId: {JobEntityId}", jobEntityId);
                throw;
            }
        }

        public async Task<JobEntity> CreateJobAsync(JobEntity job)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                context.Jobs.Add(job);
                await context.SaveChangesAsync();

                _logger.LogInformation("Job created in repository | JobId: {JobId}", job.JobId);
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create job in repository | JobId: {JobId}", job.JobId);
                throw;
            }
        }

        public async Task<JobEntity> UpdateJobAsync(JobEntity job)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                context.Jobs.Update(job);
                await context.SaveChangesAsync();

                _logger.LogInformation("Job updated in repository | JobId: {JobId}", job.JobId);
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update job in repository | JobId: {JobId}", job.JobId);
                throw;
            }
        }

        public async Task DeleteJobAsync(string jobId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var job = await context.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId);
                if (job is not null)
                {
                    // Delete all related job runs
                    var runs = await context.JobRuns
                        .Where(r => r.JobId == job.Id)
                        .ToListAsync();

                    context.JobRuns.RemoveRange(runs);

                    // Hard delete the job
                    context.Jobs.Remove(job);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Job hard-deleted in repository | JobId: {JobId} | DeletedRunsCount: {Count}", 
                        jobId, runs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete job in repository | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<List<JobRunEntity>> GetJobRunsAsync(string jobId, int limit = 50)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var runs = await context.JobRuns
                    .Where(r => r.Job.JobId == jobId && r.DeletedAt == null)
                    .OrderByDescending(r => r.StartedAt)
                    .Take(limit)
                    .ToListAsync();

                return runs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job runs in repository | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task SaveJobRunAsync(JobRunEntity run)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                context.JobRuns.Add(run);
                await context.SaveChangesAsync();

                _logger.LogInformation("Job run saved in repository | JobId: {JobId} | RunId: {RunId}", 
                    run.Job?.JobId ?? "unknown", run.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save job run in repository");
                throw;
            }
        }

        public async Task<List<JobEntity>> GetAllJobsIncludingDeletedAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var jobs = await context.Jobs.ToListAsync();
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all jobs including deleted from repository");
                throw;
            }
        }

        public async Task<bool> JobExistsAsync(string jobId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                return await context.Jobs.AnyAsync(j => j.JobId == jobId && j.DeletedAt == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if job exists in repository | JobId: {JobId}", jobId);
                throw;
            }
        }
    }
}
