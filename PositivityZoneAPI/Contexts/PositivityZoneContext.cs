using MySql.Data.MySqlClient;
using PositivityZoneAPI.Models;
using System;
using System.Collections.Generic;

namespace PositivityZoneAPI.Contexts {
    public enum Status {
        Ok,
        Maintenance,
        Development,
    }
    public class PositivityZoneContext {
        public Status Status { get; set; }
        private string ConnStr { get; set; }
        public PositivityZoneContext(string connStr) {
            ConnStr = connStr;
            Status = Status.Development;
        }

        private MySqlConnection GetConnection() {
            return new MySqlConnection(ConnStr);
        }

        public bool GetEntries(bool? approved, bool? disapproved, bool? answered, string lang, ref List<Entry> entryList) {
            using MySqlConnection conn = GetConnection();
            conn.Open();
            using MySqlCommand cmd = new MySqlCommand {
                Connection = conn,
                CommandText = "SELECT * FROM entries"
            };

            List<string> conditions = new List<string>();

            if (approved != null || answered != null || disapproved != null || !string.IsNullOrEmpty(lang)) {
                cmd.CommandText += " WHERE";
            }

            if (approved != null) {
                conditions.Add("approved = " + approved.Value.ToString());
            }

            if (disapproved != null) {
                conditions.Add("disapproved = " + disapproved.Value.ToString());
            }

            if (answered != null) {
                conditions.Add("answered = " + answered.Value.ToString());
            }

            if (!string.IsNullOrEmpty(lang)) {
                conditions.Add("language = '" + MySqlHelper.EscapeString(lang) + "'");
            }

            if (conditions.Count > 0) {
                for (int i = 0; i < conditions.Count; i++) {
                    cmd.CommandText += " " + conditions[i];
                    if (i != conditions.Count - 1) {
                        cmd.CommandText += " AND";
                    }
                }
            }

            MySqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows) {
                return false;
            }

            while (reader.Read()) {
                Entry entry = new Entry {
                    ID = Convert.ToInt32(reader["id"]),
                    Text = Convert.ToString(reader["text"]),
                    Posted = Convert.ToDateTime(reader["posted"]),
                    Approved = Convert.ToBoolean(reader["approved"]),
                    Disapproved = Convert.ToBoolean(reader["disapproved"]),
                    Language = Convert.ToString(reader["language"]),
                    Answered = Convert.ToBoolean(reader["answered"]),
                    UID = Convert.ToString(reader["uid"])
                };
                entryList.Add(entry);
            }
            return true;
        }

        public bool GetAnswers(bool? approved, bool? disapproved, int? entryId, string lang, ref List<Answer> answerList) {
            using MySqlConnection conn = GetConnection();
            conn.Open();
            using MySqlCommand cmd = new MySqlCommand {
                Connection = conn,
                CommandText = "SELECT * FROM answers"
            };

            List<string> conditions = new List<string>();

            if (approved != null || !string.IsNullOrEmpty(lang)) {
                cmd.CommandText += " WHERE";
            }

            if (approved != null) {
                conditions.Add("approved = " + approved.Value.ToString());
            }

            if (disapproved != null) {
                conditions.Add("disapproved = " + disapproved.Value.ToString());
            }

            if (entryId != null) {
                conditions.Add("entry_id = " + entryId.Value.ToString());
            }

            if (!string.IsNullOrEmpty(lang)) {
                conditions.Add("language = '" + lang + "'");
            }

            if (conditions.Count > 0) {
                for (int i = 0; i < conditions.Count; i++) {
                    cmd.CommandText += " " + conditions[i];
                    if (i != conditions.Count - 1) {
                        cmd.CommandText += " AND";
                    }
                }
            }

            MySqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows) {
                return false;
            }

            while (reader.Read()) {
                Answer answer = new Answer {
                    ID = Convert.ToInt32(reader["id"]),
                    Text = Convert.ToString(reader["text"]),
                    Posted = Convert.ToDateTime(reader["posted"]),
                    Approved = Convert.ToBoolean(reader["approved"]),
                    Disapproved = Convert.ToBoolean(reader["disapproved"]),
                    Language = Convert.ToString(reader["language"]),
                    EntryID = Convert.ToInt32(reader["entry_id"]),
                    UID = Convert.ToString(reader["uid"])
                };
                answerList.Add(answer);
            }
            return true;
        }

        public bool PostEntry(Entry entry) {
            entry.Text = MySqlHelper.EscapeString(entry.Text);
            entry.UID = MySqlHelper.EscapeString(entry.UID);
            entry.Language = MySqlHelper.EscapeString(entry.Language);
            if (entry.Language != "pl" && entry.Language != "en") {
                return false;
            }

            using (MySqlConnection conn = GetConnection()) {
                conn.Open();
                using MySqlDataAdapter adapter = new MySqlDataAdapter();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO entries (text, uid, language) VALUES (@Text, @UID, @Language);", conn);
                cmd.Parameters.Add("text", MySqlDbType.Text, entry.Text.Length, "@Text");
                cmd.Parameters.Add("uid", MySqlDbType.VarChar, 16, "@UID");
                cmd.Parameters.Add("language", MySqlDbType.VarChar, 8, "@Language");

                cmd.Parameters["text"].Value = entry.Text;
                cmd.Parameters["uid"].Value = entry.UID;
                cmd.Parameters["language"].Value = entry.Language;

                adapter.InsertCommand = cmd;
                int rowsAffected = adapter.InsertCommand.ExecuteNonQuery();
                if (rowsAffected == 0) {
                    return false;
                }
            }
            return true;
        }

        public bool PostAnswer(Answer answer) {
            if (answer.Language != "pl" && answer.Language != "en") {
                return false;
            }

            using (MySqlConnection conn = GetConnection()) {
                conn.Open();
                using var transaction = conn.BeginTransaction();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter()) {
                    MySqlCommand cmd = new MySqlCommand("INSERT INTO answers (entry_id, text, uid, language) VALUES (@EntryID, @Text, @UID, @Language);", conn);
                    cmd.Parameters.Add("@EntryID", MySqlDbType.Int32, 8).Value = answer.EntryID;
                    cmd.Parameters.Add("@Text", MySqlDbType.Text, answer.Text.Length).Value = MySqlHelper.EscapeString(answer.Text);
                    cmd.Parameters.Add("@UID", MySqlDbType.VarChar, 32).Value = MySqlHelper.EscapeString(answer.UID);
                    cmd.Parameters.Add("@Language", MySqlDbType.VarChar, 8).Value = MySqlHelper.EscapeString(answer.Language);

                    adapter.InsertCommand = cmd;
                    int rowsAffected = adapter.InsertCommand.ExecuteNonQuery();
                    if (rowsAffected == 0) {
                        transaction.Rollback();
                        return false;
                    }
                }
                using (MySqlDataAdapter adapter = new MySqlDataAdapter()) {
                    MySqlCommand cmd = new MySqlCommand("UPDATE entries SET answered = true WHERE id = @EntryID;", conn);
                    cmd.Parameters.Add("@EntryID", MySqlDbType.Int32, 8).Value = answer.EntryID;
                    adapter.UpdateCommand = cmd;
                    int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
                    if (rowsAffected == 0) {
                        transaction.Rollback();
                        return false;
                    }
                }
                transaction.Commit();
            }
            return true;
        }

        public bool PostUser(string uid) {
            using MySqlConnection conn = GetConnection();
            conn.Open();
            if (UserExists(uid)) {
                using MySqlDataAdapter adapter = new MySqlDataAdapter();
                MySqlCommand cmd = new MySqlCommand("UPDATE users SET lastactive = current_timestamp() WHERE uid = @UID;", conn);
                cmd.Parameters.Add("@UID", MySqlDbType.VarChar, 32).Value = uid;
                adapter.UpdateCommand = cmd;
                int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
                if (rowsAffected == 0) {
                    return false;
                }

                return true;
            }
            else {
                using MySqlDataAdapter adapter = new MySqlDataAdapter();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO users (uid) VALUES (@UID);", conn);
                cmd.Parameters.Add("@UID", MySqlDbType.VarChar, 32).Value = MySqlHelper.EscapeString(uid);
                adapter.InsertCommand = cmd;
                int rowsAffected = adapter.InsertCommand.ExecuteNonQuery();
                if (rowsAffected == 0) {
                    return false;
                }

                return true;
            }
        }

        public bool HasPass(string uid) {
            using (MySqlConnection conn = GetConnection()) {
                conn.Open();
                using MySqlCommand cmd = new MySqlCommand("SELECT hash FROM users WHERE uid = @UID;", conn);
                cmd.Parameters.Add("uid", MySqlDbType.VarChar, uid.Length, "@UID");
                cmd.Parameters["uid"].Value = MySqlHelper.EscapeString(uid);

                MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.HasRows) {
                    return false;
                }

                reader.Read();
                if (Convert.ToString(reader["hash"]) == string.Empty) {
                    return false;
                }
            }
            return true;
        }

        private bool UserExists(string uid) {
            using (MySqlConnection conn = GetConnection()) {
                conn.Open();
                using MySqlCommand command = new MySqlCommand("SELECT uid FROM users;", conn);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) {
                    if (Convert.ToString(reader["uid"]) == uid) {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Recover(string uid, string hash) {
            using (MySqlConnection conn = GetConnection()) {
                conn.Open();
                using MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE banned = false AND uid = @UID", conn);
                cmd.Parameters.Add("uid", MySqlDbType.VarChar, 32, "@UID");
                cmd.Parameters["uid"].Value = MySqlHelper.EscapeString(uid);
                MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.HasRows) {
                    return false;
                }

                reader.Read();
                if (Convert.ToString(reader["hash"]) != hash) {
                    return false;
                }
            }
            return true;
        }

        public bool ChangePass(string uid, string oldHash, string newHash) {
            using MySqlConnection conn = GetConnection();
            conn.Open();

            using MySqlCommand cmd = new MySqlCommand("UPDATE users SET hash = @NewHash WHERE uid = @UID AND hash = @OldHash", conn);
            cmd.Parameters.Add("uid", MySqlDbType.VarChar, 32, "@UID");
            cmd.Parameters.Add("newhash", MySqlDbType.VarChar, 64, "@NewHash");
            cmd.Parameters.Add("oldhash", MySqlDbType.VarChar, 64, "@OldHash");

            cmd.Parameters["uid"].Value = MySqlHelper.EscapeString(uid);
            cmd.Parameters["newhash"].Value = MySqlHelper.EscapeString(newHash);
            cmd.Parameters["oldhash"].Value = MySqlHelper.EscapeString(oldHash);

            using MySqlDataAdapter adapter = new MySqlDataAdapter {
                UpdateCommand = cmd
            };

            int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
            if (rowsAffected != 1) {
                return false;
            }
            else {
                return true;
            }
        }

        public bool SetPass(string uid, string hash) {
            using MySqlConnection conn = GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("UPDATE users SET hash = @Hash WHERE uid = @UID AND hash IS NULL", conn);
            cmd.Parameters.Add("hash", MySqlDbType.VarChar, 64, "@Hash");
            cmd.Parameters.Add("uid", MySqlDbType.VarChar, 32, "@UID");
            cmd.Parameters["hash"].Value = MySqlHelper.EscapeString(hash);
            cmd.Parameters["uid"].Value = MySqlHelper.EscapeString(uid);

            using MySqlDataAdapter adapter = new MySqlDataAdapter() {
                UpdateCommand = cmd
            };

            int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
            if (rowsAffected != 1) {
                return false;
            }
            else {
                return true;
            }
        }

        public bool ApproveEntry(int id) {
            using MySqlConnection conn = GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("UPDATE entries SET approved = true WHERE id = @ID AND approved = false AND disapproved = false;", conn);
            cmd.Parameters.Add("@ID", MySqlDbType.Int32, 8).Value = id;
            using MySqlDataAdapter adapter = new MySqlDataAdapter() {
                UpdateCommand = cmd
            };

            int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
            if (rowsAffected != 1) {
                return false;
            }
            else {
                return true;
            }
        }

        public bool DisapproveEntry(int id) {
            using MySqlConnection conn = GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("UPDATE entries SET approved = false, disapproved = true WHERE id = @ID AND approved = false AND disapproved = false", conn);
            cmd.Parameters.Add("@ID", MySqlDbType.Int32, 8).Value = id;
            using MySqlDataAdapter adapter = new MySqlDataAdapter() {
                UpdateCommand = cmd
            };

            int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
            if (rowsAffected != 1) {
                return false;
            }
            else {
                return true;
            }
        }

        public bool ApproveAnswer(int id) {
            using MySqlConnection conn = GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("UPDATE answers SET approved = true WHERE id = @ID AND approved = false AND disapproved = false", conn);
            cmd.Parameters.Add("@ID", MySqlDbType.Int32, 8).Value = id;
            using MySqlDataAdapter adapter = new MySqlDataAdapter() {
                UpdateCommand = cmd
            };

            int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
            if (rowsAffected != 1) {
                return false;
            }
            else {
                return true;
            }
        }

        public bool DisapproveAnswer(int id) {
            using MySqlConnection conn = GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("UPDATE answers SET approved = false, disapproved = true WHERE id = @ID AND approved = false AND disapproved = false", conn);
            cmd.Parameters.Add("@ID", MySqlDbType.Int32, 8).Value = id;
            using MySqlDataAdapter adapter = new MySqlDataAdapter() {
                UpdateCommand = cmd
            };

            int rowsAffected = adapter.UpdateCommand.ExecuteNonQuery();
            if (rowsAffected != 1) {
                return false;
            }
            else {
                return true;
            }
        }
    }
}
