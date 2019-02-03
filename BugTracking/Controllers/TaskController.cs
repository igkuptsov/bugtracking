using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracking.Models;

namespace BugTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        public const string TotalCountKey = "X-Total-Count";
        private readonly BugTrackingContext _context;

        public TaskController(BugTrackingContext context)
        {
            _context = context;
        }

        // GET: api/Task
        [HttpGet]
        public IEnumerable<TaskItem> GetTaskItem(int projectId, int? skip = null, int? take = null, string sortField = null, int? sortDirection = null, int? priorityFilter = null, DateTime? createdFrom = null, DateTime? createdTo = null)
        {            
            var _taskItem = _context.TaskItem.Where(i => i.ProjectId == projectId);

            //filter logic
            if (priorityFilter.HasValue)
            {
                _taskItem = _taskItem.Where(i => i.TaskPriority == priorityFilter.Value);
            }

            if (createdFrom.HasValue)
            {
                _taskItem = _taskItem.Where(i => i.TaskDateCreated >= createdFrom.Value.Date);
            }

            if (createdTo.HasValue)
            {
                _taskItem = _taskItem.Where(i => i.TaskDateCreated < createdTo.Value.Date.AddDays(1));
            }

            //get totals for pager
            Response.Headers.Add(TotalCountKey, _taskItem.Count().ToString());

            //sort logic
            if (sortField == nameof(TaskItem.TaskDateCreated))
            {
                _taskItem = (!sortDirection.HasValue || sortDirection == 0) ? _taskItem.OrderBy(i => i.TaskDateCreated) : _taskItem.OrderByDescending(i => i.TaskDateCreated);
            }
            else if (sortField == nameof(TaskItem.TaskPriority))
            {
                _taskItem = (!sortDirection.HasValue || sortDirection == 0) ? _taskItem.OrderBy(i => i.TaskPriority) : _taskItem.OrderByDescending(i => i.TaskPriority);
            }
            else //default sort
            {
                _taskItem = (!sortDirection.HasValue || sortDirection == 0) ? _taskItem.OrderBy(i => i.TaskDateCreated) : _taskItem.OrderByDescending(i => i.TaskDateCreated);
            }


            //pager logic
            if (skip.HasValue)
            {
                _taskItem = _taskItem.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                _taskItem = _taskItem.Take(take.Value);
            }

            return _taskItem;
        }

        // GET: api/Task/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var taskItem = await _context.TaskItem.FindAsync(id);

            if (taskItem == null)
            {
                return NotFound();
            }

            return Ok(taskItem);
        }

        // PUT: api/Task/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaskItem([FromRoute] int id, [FromBody] TaskItem taskItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != taskItem.TaskId)
            {
                return BadRequest();
            }

            var _taskItem = await _context.TaskItem.FindAsync(id);

            if (_taskItem == null)
            {
                return NotFound();
            }

            if (_taskItem.TaskStatus == Enums.TaskStatus.Closed)
            {
                return BadRequest();
            }

            _taskItem.TaskName = taskItem.TaskName;
            _taskItem.TaskDescription = taskItem.TaskDescription;
            _taskItem.TaskPriority = taskItem.TaskPriority;
            _taskItem.TaskStatus = taskItem.TaskStatus;
            _taskItem.TaskDateUpdated = DateTime.Now;

            _context.Entry(_taskItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Task
        [HttpPost]
        public async Task<IActionResult> PostTaskItem([FromBody] TaskItem taskItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            taskItem.TaskDateCreated = DateTime.Now;
            taskItem.TaskDateUpdated = taskItem.TaskDateCreated;
            taskItem.TaskStatus = Enums.TaskStatus.New;

            _context.TaskItem.Add(taskItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTaskItem", new { id = taskItem.TaskId }, taskItem);
        }

        // DELETE: api/Task/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var taskItem = await _context.TaskItem.FindAsync(id);
            if (taskItem == null)
            {
                return NotFound();
            }

            _context.TaskItem.Remove(taskItem);
            await _context.SaveChangesAsync();

            return Ok(taskItem);
        }

        private bool TaskItemExists(int id)
        {
            return _context.TaskItem.Any(e => e.TaskId == id);
        }
    }
}