using System;
using System.Collections.Generic;

namespace belt.Models
{
    public class Display
    {
        public int DisplayID {get;set;}
        public string UserName {get;set;}

         public string ActivityName {get;set;}
        public DateTime Date {get;set;}

        public TimeSpan Time {get;set;}

        public int Duration {get;set;}

        public string　Description {get;set;}
        public List<User> Guests {get;set;}

        public User Creator {get;set;}


        public bool IsHosting {get;set;}
        public bool IsAttending {get;set;}
        

    }
}