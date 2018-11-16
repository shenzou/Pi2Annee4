using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Test
{
    class DBManagement
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;
        private string port;

        public DBManagement()
        {
            Initialize();
        }

        private void Initialize()
        {
            Credentials cred = new Credentials();
            server = cred.DBServer;
            port = cred.Dbport;
            database = "PI2";
            uid = cred.DBLogin;
            password = cred.DBPassword;
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                Console.WriteLine("Connecté à la base de données.");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Erreur de log base de donnée: " + e);
            }
            return false;
        }

        //Close connection
        private bool CloseConnection()
        {
            return false;
        }

        //Insert statement
        public void Insert()
        {
        }

        //Update statement
        public void Update()
        {
        }

        //Delete statement
        public void Delete()
        {
        }

        /*
        //Select statement
        public List<string>[] Select()
        {

        }
        */

        public void AddUserLog(string log)
        {
            try
            {
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                OpenConnection();
                string request = "INSERT INTO PI2.userlog(userInput, dateTime) VALUES('" + log + "','" + dateTime + "'); ";
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = request;
                MySqlDataReader reader;
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    Console.WriteLine(reader.GetString(0));
                }
                else Console.WriteLine("Userlog ajouté.");
                connection.Close();
            }
            catch
            {
                Console.WriteLine("Erreur avec la base de données. Userlog non ajouté.");
            }
        }

        public void AddRequestHistory(string json)
        {
            try
            {
                OpenConnection();
                string request = "INSERT INTO PI2.historyrequest(json) VALUES('" + json + "'); ";
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = request;
                MySqlDataReader reader;
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    Console.WriteLine(reader.GetString(0));
                }
                else Console.WriteLine("Historique ajouté.");
                connection.Close();
            }
            catch
            {
                Console.WriteLine("Erreur avec la base de données. Historique non ajouté.");
            }
        }

        public void AddVoiceTranscript(string text)
        {
            try
            {
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh");
                OpenConnection();
                string request = "INSERT INTO PI2.voicetranscripted(text, date) VALUES('" + text + "','" + dateTime + "'); ";
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = request;
                MySqlDataReader reader;
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    Console.WriteLine(reader.GetString(0));
                }
                else Console.WriteLine("Transcription de voix ajoutée.");
                connection.Close();
            }
            catch
            {
                Console.WriteLine("Erreur avec la base de données. Transcription non ajoutée.");
            }
        }

        public string LastVoiceTranscript()
        {
            OpenConnection();
            string request = "SELECT text FROM voicetranscripted WHERE id=(SELECT MAX(id) FROM voicetranscripted);";
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = request;

            MySqlDataReader reader;
            reader = command.ExecuteReader();
            string text = "";
            while(reader.Read())
            {
                try
                {
                    text = reader.GetString(0);
                }
                catch
                {
                    Console.WriteLine("Erreur lors de la lecture du dernier élement.");
                }
            }
            return text;
        }

        //Count statement
        public int Count()
        {
            return 0;
        }
    }
}
