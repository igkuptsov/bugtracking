using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugTracking.Models
{
    [Table("Project")]
    public class ProjectItem
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        public string ProjectName { get; set; }

        public string ProjectDescription { get; set; }

        public DateTime ProjectDateCreated { get; set; }

        public DateTime ProjectDateUpdated { get; set; }

        public List<TaskItem> Tasks { get; set; }
    }
}
