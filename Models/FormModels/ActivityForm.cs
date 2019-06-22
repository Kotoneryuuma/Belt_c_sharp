using System;
using System.ComponentModel.DataAnnotations;

namespace belt.Models
{
    public class ActivityForm
    {
        [Required]
        [MinLength(2)]
        [Display(Name = "Title")]
        public string Title {get;set;}

        [Required]
        [Display(Name = "Time")]
        [DataType(DataType.Time)]

        public TimeSpan Time {get;set;}

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date {get;set;}

        [Required]
      
        public int Duration {get;set;}

        [Required]
        [MinLength(2)]
        public string Description {get;set;}





    }
}