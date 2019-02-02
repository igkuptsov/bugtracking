using BugTracking.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugTracking.Models
{
    [Table("Tasks")]
    public class TaskItem
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        public string TaskName { get; set; }

        public string TaskDescription { get; set; }

        public DateTime TaskDateCreated { get; set; }

        public DateTime TaskDateUpdated { get; set; }

        [Required]
        public TaskStatus TaskStatus { get; set; }

        [Range(1, int.MaxValue)]
        [Required]
        public int TaskPriority { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public ProjectItem Project { get; set; }
    }
}
