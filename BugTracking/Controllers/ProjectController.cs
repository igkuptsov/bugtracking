using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracking.Models;

namespace BugTracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        public const string TotalCountKey = "X-Total-Count";

        private readonly BugTrackingContext _context;

        public ProjectController(BugTrackingContext context)
        {
            _context = context;
        }

        // GET: api/Project
        [HttpGet]
        public IEnumerable<ProjectItem> GetProjectItems(int? skip = null, int? take = null)
        {
            var _projectItems = _context.ProjectItems.OrderBy(x => x.ProjectDateCreated).AsQueryable();

            //get totals for pager
            Response.Headers.Add(TotalCountKey, _projectItems.Count().ToString());

            //pager logic
            if (skip.HasValue)
            {
                _projectItems = _projectItems.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                _projectItems = _projectItems.Take(take.Value);
            }

            return _projectItems;
        }

        // GET: api/Project/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var projectItem = await _context.ProjectItems.FindAsync(id);

            if (projectItem == null)
            {
                return NotFound();
            }

            return Ok(projectItem);
        }

        // PUT: api/Project/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProjectItem([FromRoute] int id, [FromBody] ProjectItem projectItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != projectItem.ProjectId)
            {
                return BadRequest();
            }

            var _projectItem = await _context.ProjectItems.FindAsync(id);
            if (_projectItem == null)
            {
                return NotFound();
            }

            _projectItem.ProjectName = projectItem.ProjectName;
            _projectItem.ProjectDescription = projectItem.ProjectDescription;
            _projectItem.ProjectDateUpdated = DateTime.Now;

            _context.Entry(_projectItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectItemExists(id))
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

        // POST: api/Project
        [HttpPost]
        public async Task<IActionResult> PostProjectItem([FromBody] ProjectItem projectItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            projectItem.ProjectDateCreated = DateTime.Now;
            projectItem.ProjectDateUpdated = projectItem.ProjectDateCreated;

            _context.ProjectItems.Add(projectItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProjectItem", new { id = projectItem.ProjectId }, projectItem);
        }

        // DELETE: api/Project/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProjectItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var projectItem = await _context.ProjectItems.FindAsync(id);
            if (projectItem == null)
            {
                return NotFound();
            }

            _context.ProjectItems.Remove(projectItem);
            await _context.SaveChangesAsync();

            return Ok(projectItem);
        }

        private bool ProjectItemExists(int id)
        {
            return _context.ProjectItems.Any(e => e.ProjectId == id);
        }
    }
}