using System;

namespace PositivityZoneAPI.Models {
    public class Answer {
        public int ID { get; set; }
        public int EntryID { get; set; }
        public string Text { get; set; }
        public DateTime Posted { get; set; }
        public bool Approved { get; set; }
        public bool Disapproved { get; set; }
        public string Language { get; set; }
        public string UID { get; set; }
    }
}
