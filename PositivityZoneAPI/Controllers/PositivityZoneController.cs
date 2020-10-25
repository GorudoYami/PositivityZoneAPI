using Microsoft.AspNetCore.Mvc;
using PositivityZoneAPI.Contexts;
using PositivityZoneAPI.Filters;
using PositivityZoneAPI.Models;
using System;
using System.Collections.Generic;

namespace PositivityZoneAPI.Controllers {

    [Route("api")]
    [ApiController]
    [ApiKeyAuth]
    public class PositivityZoneController : ControllerBase {
        private readonly PositivityZoneContext context;

        public PositivityZoneController(PositivityZoneContext _context) {
            context = _context;
        }

        [HttpGet("entries")]
        public ActionResult<List<Entry>> GetEntries(bool? approved, bool? disapproved, bool? answered, string lang) {
            List<Entry> entryList = new List<Entry>();
            if (context.GetEntries(approved, disapproved, answered, lang, ref entryList)) {
                return entryList;
            }
            else {
                return BadRequest();
            }
        }

        [HttpPost("entry")]
        public ActionResult PostEntry(Entry entry) {
            if (context.PostEntry(entry)) {
                return NoContent();
            }
            else {
                return BadRequest();
            }
        }


        [HttpGet("answers")]
        public ActionResult<List<Answer>> GetAnswers(bool? approved, bool? disapproved, int? entryId, string lang) {
            List<Answer> answerList = new List<Answer>();
            if (context.GetAnswers(approved, disapproved, entryId, lang, ref answerList)) {
                return answerList;
            }
            else {
                return NoContent();
            }
        }

        [HttpPost("answer")]
        public ActionResult PostAnswer(Answer answer) {
            if (context.PostAnswer(answer)) {
                return NoContent();
            }
            else {
                return BadRequest();
            }
        }

        [HttpGet("answer/approve")]
        public ActionResult ApproveAnswer(int id) {
            if (context.ApproveAnswer(id)) {
                return Ok();
            }
            else {
                return BadRequest();
            }
        }

        [HttpGet("answer/disapprove")]
        public ActionResult DisapproveAnswer(int id) {
            if (context.DisapproveAnswer(id)) {
                return Ok();
            }
            else {
                return BadRequest();
            }
        }

        [HttpGet("entry/approve")]
        public ActionResult ApproveEntry(int id) {
            if (context.ApproveEntry(id)) {
                return Ok();
            }
            else {
                return BadRequest();
            }
        }

        [HttpGet("entry/disapprove")]
        public ActionResult DisapproveEntry(int id) {
            if (context.DisapproveEntry(id)) {
                return Ok();
            }
            else {
                return BadRequest();
            }
        }

        [HttpPost("user")]
        public ActionResult PostUser(dynamic uid) {
            if (context.PostUser(Convert.ToString(uid))) {
                return NoContent();
            }
            else {
                return BadRequest();
            }
        }

        [HttpGet("user/haspass")]
        public ActionResult<bool> HasPass(string uid) {
            return context.HasPass(uid);
        }

        [HttpGet("user/recover")]
        public ActionResult<bool> Recover(string uid, string hash) {
            return context.Recover(uid, hash);
        }

        [HttpGet("user/changepass")]
        public ActionResult<bool> ChangePass(string uid, string oldHash, string newHash) {
            return context.ChangePass(uid, oldHash, newHash);
        }

        [HttpGet("user/setpass")]
        public ActionResult<bool> SetPass(string hash, string uid) {
            return context.SetPass(uid, hash);
        }

        [HttpGet("status")]
        public ActionResult<Status> GetStatus() {
            return context.Status;
        }
    }
}
