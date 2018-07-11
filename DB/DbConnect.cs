using cjlogisticsChatBot.Dialogs;
using cjlogisticsChatBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

namespace cjlogisticsChatBot.DB
{
    public class DbConnect
    {
        static Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/");
        const string CONSTRINGNAME = "conString";
        //ConnectionStringSettings connStr = rootWebConfig.ConnectionStrings.ConnectionStrings[CONSTRINGNAME]
        string connStr = rootWebConfig.ConnectionStrings.ConnectionStrings[CONSTRINGNAME].ToString();
        //string connStr = "Data Source=taiholab.database.windows.net;Initial Catalog=taihoLab_2;User ID=taihoinst;Password=taiho9788!;";
        //string connStr = "Data Source=10.6.222.21,1433;Initial Catalog=konadb;User ID=konadb;Password=Didwoehd20-9!;";
        //StringBuilder sb = new StringBuilder();
        public readonly string TEXTDLG = "2";
        public readonly string CARDDLG = "3";
        public readonly string MEDIADLG = "4";

        public void ConnectDb()
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(connStr);
                conn.Open();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

        }


        public List<DialogList> SelectInitDialog(String channel)
        {
            SqlDataReader rdr = null;
            List<DialogList> dialogs = new List<DialogList>();
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT   				    ";
                cmd.CommandText += " 	DLG_ID,                 ";
                cmd.CommandText += " 	DLG_TYPE,               ";
                cmd.CommandText += " 	DLG_GROUP,              ";
                cmd.CommandText += " 	DLG_ORDER_NO            ";
                cmd.CommandText += " FROM TBL_DLG     ";
                cmd.CommandText += " WHERE DLG_GROUP = '1'      ";
                cmd.CommandText += " AND USE_YN = 'Y'           ";
                cmd.CommandText += " ORDER BY DLG_ID            ";

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                DButil.HistoryLog(" db SelectInitDialog !! ");


                while (rdr.Read())
                {
                    DialogList dlg = new DialogList();
                    dlg.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    dlg.dlgType = rdr["DLG_TYPE"] as string;
                    dlg.dlgGroup = rdr["DLG_GROUP"] as string;
                    dlg.dlgOrderNo = rdr["DLG_ORDER_NO"] as string;

                    using (SqlConnection conn2 = new SqlConnection(connStr))
                    {
                        SqlCommand cmd2 = new SqlCommand();
                        conn2.Open();
                        cmd2.Connection = conn2;
                        SqlDataReader rdr2 = null;
                        if (dlg.dlgType.Equals(TEXTDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                            }
                            rdr2.Close();
                        }
                        else if (dlg.dlgType.Equals(CARDDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_SUBTITLE, CARD_TEXT, IMG_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT, " +
                                    "CARD_DIVISION, CARD_VALUE " +
                                    "FROM TBL_DLG_CARD WHERE DLG_ID = @dlgID AND USE_YN = 'Y' ";
                            //if (channel.Equals("facebook"))
                            //{
                            //    cmd2.CommandText += "FB_USE_YN = 'Y' ";
                            //}
                            cmd2.CommandText += "ORDER BY CARD_ORDER_NO";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);
                            List<CardList> dialogCards = new List<CardList>();
                            while (rdr2.Read())
                            {
                                CardList dlgCard = new CardList();
                                dlgCard.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlgCard.cardSubTitle = rdr2["CARD_SUBTITLE"] as string;
                                dlgCard.cardText = rdr2["CARD_TEXT"] as string;
                                dlgCard.imgUrl = rdr2["IMG_URL"] as string;
                                dlgCard.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlgCard.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlgCard.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlgCard.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlgCard.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlgCard.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlgCard.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlgCard.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlgCard.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlgCard.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlgCard.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlgCard.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                                dlgCard.cardDivision = rdr2["CARD_DIVISION"] as string;
                                dlgCard.cardValue = rdr2["CARD_VALUE"] as string;
                                dialogCards.Add(dlgCard);
                            }
                            dlg.dialogCard = dialogCards;
                        }
                        else if (dlg.dlgType.Equals(MEDIADLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT, MEDIA_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT " +
                                    "FROM TBL_DLG_MEDIA WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                                dlg.mediaUrl = rdr2["MEDIA_URL"] as string;
                                dlg.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlg.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlg.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlg.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlg.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlg.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlg.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlg.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlg.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlg.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlg.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlg.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                            }
                        }

                    }
                    dialogs.Add(dlg);
                }
                rdr.Close();
            }
            return dialogs;
        }

        public DialogList SelectDialog(int dlgID)
        {
            SqlDataReader rdr = null;
            DialogList dlg = new DialogList();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += " SELECT   				                    ";
                cmd.CommandText += " 	A.DLG_ID,                               ";
                cmd.CommandText += " 	A.DLG_NAME,                             ";
                cmd.CommandText += " 	A.DLG_DESCRIPTION,                      ";
                cmd.CommandText += " 	A.DLG_LANG,                             ";
                cmd.CommandText += " 	A.DLG_TYPE,                             ";
                cmd.CommandText += " 	A.DLG_ORDER_NO,                         ";
                cmd.CommandText += " 	A.DLG_GROUP                             ";
                cmd.CommandText += " FROM TBL_DLG A, TBL_DLG_RELATION_LUIS B    ";
                cmd.CommandText += " WHERE A.DLG_ID = B.DLG_ID                  ";
                cmd.CommandText += "   AND A.DLG_ID = @dlgId                    ";
                cmd.CommandText += "   AND A.USE_YN = 'Y'                       ";
                cmd.CommandText += " ORDER BY  A.DLG_ORDER_NO                   ";

                cmd.Parameters.AddWithValue("@dlgID", dlgID);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    dlg.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    dlg.dlgType = rdr["DLG_TYPE"] as string;
                    dlg.dlgGroup = rdr["DLG_GROUP"] as string;
                    dlg.dlgOrderNo = rdr["DLG_ORDER_NO"] as string;

                    using (SqlConnection conn2 = new SqlConnection(connStr))
                    {
                        SqlCommand cmd2 = new SqlCommand();
                        conn2.Open();
                        cmd2.Connection = conn2;
                        SqlDataReader rdr2 = null;
                        if (dlg.dlgType.Equals(TEXTDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                            }
                            rdr2.Close();
                        }
                        else if (dlg.dlgType.Equals(CARDDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_SUBTITLE, CARD_TEXT, IMG_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT, " +
                                    "CARD_DIVISION, CARD_VALUE, CARD_ORDER_NO " +
                                    "FROM TBL_DLG_CARD WHERE DLG_ID = @dlgID AND USE_YN = 'Y' ORDER BY CARD_ORDER_NO";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);
                            List<CardList> dialogCards = new List<CardList>();
                            while (rdr2.Read())
                            {
                                CardList dlgCard = new CardList();
                                dlgCard.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlgCard.cardSubTitle = rdr2["CARD_SUBTITLE"] as string;
                                dlgCard.cardText = rdr2["CARD_TEXT"] as string;
                                dlgCard.imgUrl = rdr2["IMG_URL"] as string;
                                dlgCard.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlgCard.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlgCard.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlgCard.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlgCard.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlgCard.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlgCard.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlgCard.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlgCard.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlgCard.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlgCard.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlgCard.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                                dlgCard.cardDivision = rdr2["CARD_DIVISION"] as string;
                                dlgCard.cardValue = rdr2["CARD_VALUE"] as string;
                                //dlgCard.card_order_no = rdr2["CARD_ORDER_NO"] as string;
                                dlgCard.card_order_no = Convert.ToInt32(rdr2["CARD_ORDER_NO"]);

                                dialogCards.Add(dlgCard);
                            }
                            dlg.dialogCard = dialogCards;
                        }
                        else if (dlg.dlgType.Equals(MEDIADLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT, MEDIA_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT , CARD_DIVISION, CARD_VALUE " +
                                    "FROM TBL_DLG_MEDIA WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                                dlg.mediaUrl = rdr2["MEDIA_URL"] as string;
                                dlg.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlg.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlg.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlg.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlg.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlg.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlg.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlg.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlg.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlg.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlg.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlg.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                                dlg.cardDivision = rdr2["CARD_DIVISION"] as string;
                                dlg.cardValue = rdr2["CARD_VALUE"] as string;
                            }
                        }

                    }
                }
            }
            return dlg;
        }

        public List<CardList> SelectDialogCard(int dlgID)
        {
            SqlDataReader rdr = null;
            List<CardList> dialogCard = new List<CardList>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT CARD_DLG_ID, DLG_ID, CARD_TITLE, CARD_SUBTITLE, CARD_TEXT, IMG_URL," +
                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, " +
                    "CARD_DIVISION, CARD_VALUE " +
                    "FROM TBL_DLG_CARD WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999 ORDER BY CARD_ORDER_NO";
                //"FROM TBL_SECCS_DLG_CARD WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999 ORDER BY CARD_ORDER_NO";

                cmd.Parameters.AddWithValue("@dlgID", dlgID);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    int cardDlgId = Convert.ToInt32(rdr["CARD_DLG_ID"]);
                    int dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    string cardTitle = rdr["CARD_TITLE"] as string;
                    string cardSubTitle = rdr["CARD_SUBTITLE"] as string;
                    string cardText = rdr["CARD_TEXT"] as string;
                    string imgUrl = rdr["IMG_URL"] as string;
                    string btn1Type = rdr["BTN_1_TYPE"] as string;
                    string btn1Title = rdr["BTN_1_TITLE"] as string;
                    string btn1Context = rdr["BTN_1_CONTEXT"] as string;
                    string btn2Type = rdr["BTN_2_TYPE"] as string;
                    string btn2Title = rdr["BTN_2_TITLE"] as string;
                    string btn2Context = rdr["BTN_2_CONTEXT"] as string;
                    string btn3Type = rdr["BTN_3_TYPE"] as string;
                    string btn3Title = rdr["BTN_3_TITLE"] as string;
                    string btn3Context = rdr["BTN_3_CONTEXT"] as string;
                    string cardDivision = rdr["CARD_DIVISION"] as string;
                    string cardValue = rdr["CARD_VALUE"] as string;

                    CardList dlgCard = new CardList();
                    dlgCard.cardDlgId = cardDlgId;
                    dlgCard.dlgId = dlgId;
                    dlgCard.cardTitle = cardTitle;
                    dlgCard.cardSubTitle = cardSubTitle;
                    dlgCard.cardText = cardText;
                    dlgCard.imgUrl = imgUrl;
                    dlgCard.btn1Type = btn1Type;
                    dlgCard.btn1Title = btn1Title;
                    dlgCard.btn1Context = btn1Context;
                    dlgCard.btn2Type = btn2Type;
                    dlgCard.btn2Title = btn2Title;
                    dlgCard.btn2Context = btn2Context;
                    dlgCard.btn3Type = btn3Type;
                    dlgCard.btn3Title = btn3Title;
                    dlgCard.btn3Context = btn3Context;
                    dlgCard.cardDivision = cardDivision;
                    dlgCard.cardValue = cardValue;

                    dialogCard.Add(dlgCard);
                }
            }
            return dialogCard;
        }

        public List<TextList> SelectDialogText(int dlgID)
        {
            SqlDataReader rdr = null;
            List<TextList> dialogText = new List<TextList>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";
                //cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_SECCS_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";

                cmd.Parameters.AddWithValue("@dlgID", dlgID);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    int textDlgId = Convert.ToInt32(rdr["TEXT_DLG_ID"]);
                    int dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    string cardTitle = rdr["CARD_TITLE"] as string;
                    string cardText = rdr["CARD_TEXT"] as string;


                    TextList dlgText = new TextList();
                    dlgText.textDlgId = textDlgId;
                    dlgText.dlgId = dlgId;
                    dlgText.cardTitle = cardTitle;
                    dlgText.cardText = cardText;


                    dialogText.Add(dlgText);
                }
            }
            return dialogText;
        }


        public List<TextList> SelectSorryDialogText(string dlgGroup)
        {
            SqlDataReader rdr = null;
            List<TextList> dialogText = new List<TextList>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE,CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = (SELECT DLG_ID FROM TBL_DLG WHERE DLG_GROUP = @dlgGroup) AND USE_YN = 'Y'";
                //cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_SECCS_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";

                cmd.Parameters.AddWithValue("@dlgGroup", dlgGroup);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    int textDlgId = Convert.ToInt32(rdr["TEXT_DLG_ID"]);
                    int dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    string cardTitle = rdr["CARD_TITLE"] as string;
                    string cardText = rdr["CARD_TEXT"] as string;


                    TextList dlgText = new TextList();
                    dlgText.textDlgId = textDlgId;
                    dlgText.dlgId = dlgId;
                    dlgText.cardTitle = cardTitle;
                    dlgText.cardText = cardText;


                    dialogText.Add(dlgText);
                }
            }
            return dialogText;
        }


        //KSO START
        public CardList BannedChk(string orgMent)
        {
            SqlDataReader rdr = null;
            CardList SelectBanned = new CardList();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT                                                                                                                                                         ";
                cmd.CommandText += " TOP 1 TD.DLG_ID, (SELECT TOP 1 BANNED_WORD FROM TBL_BANNED_WORD_LIST WHERE CHARINDEX(BANNED_WORD, @msg) > 0) AS BANNED_WORD, TDT.CARD_TITLE, TDT.CARD_TEXT     ";
                cmd.CommandText += " FROM TBL_DLG TD, TBL_DLG_TEXT TDT                                                                                                                              ";
                cmd.CommandText += " WHERE TD.DLG_ID = TDT.DLG_ID                                                                                                                                   ";
                cmd.CommandText += " AND                                                                                                                                                            ";
                cmd.CommandText += " 	TD.DLG_GROUP =                                                                                                                                              ";
                cmd.CommandText += " 	(                                                                                                                                                           ";
                cmd.CommandText += " 	   SELECT CASE WHEN SUM(CASE WHEN BANNED_WORD_TYPE = 3 THEN CHARINDEX(A.BANNED_WORD, @msg) END) > 0 THEN 3                                                  ";
                cmd.CommandText += " 			  WHEN SUM(CASE WHEN BANNED_WORD_TYPE = 4 THEN CHARINDEX(A.BANNED_WORD, @msg) END) > 0 THEN 4                                                       ";
                cmd.CommandText += " 			 END                                                                                                                                                ";
                cmd.CommandText += " 	   FROM TBL_BANNED_WORD_LIST A                                                                                                                              ";
                cmd.CommandText += " 	) AND TD.DLG_GROUP IN (3,4)                                                                                                                                 ";
                cmd.CommandText += " ORDER BY NEWID()                                                                                                                                               ";

                cmd.Parameters.AddWithValue("@msg", orgMent);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rdr.Read())
                {
                    //answerMsg = rdr["CARD_TEXT"] + "@@" + rdr["DLG_ID"] + "@@" + rdr["CARD_TITLE"];

                    int dlg_id = Convert.ToInt32(rdr["DLG_ID"]);
                    String card_title = rdr["CARD_TITLE"] as String;
                    String card_text = rdr["CARD_TEXT"] as String;

                    SelectBanned.dlgId = dlg_id;
                    SelectBanned.cardTitle = card_title;
                    SelectBanned.cardText = card_text;
                }
            }
            return SelectBanned;
        }

        public CacheList CacheChk(string orgMent)
        {
            SqlDataReader rdr = null;
            CacheList result = new CacheList();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, ISNULL(LUIS_INTENT_SCORE,'') AS LUIS_INTENT_SCORE FROM TBL_QUERY_ANALYSIS_RESULT WHERE LOWER(QUERY) = LOWER(@msg) AND RESULT ='H'";

                cmd.Parameters.AddWithValue("@msg", orgMent);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string luisId = rdr["LUIS_ID"] as String;
                    string intentId = rdr["LUIS_INTENT"] as String;
                    string entitiesId = rdr["LUIS_ENTITIES"] as String;
                    string luisScore = rdr["LUIS_INTENT_SCORE"] as String;


                    result.luisId = luisId;
                    result.luisIntent = intentId;
                    result.luisEntities = entitiesId;
                    result.luisScore = luisScore;
                }
            }
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 이부분(수정해야함)
        public String ContextChk(string luisIntent)
        {
            SqlDataReader rdr = null;
            string result = "";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, DLG_ID, ContextLabel, MissingEntities  ";
                cmd.CommandText += " FROM TBL_DLG_RELATION_LUIS                                                         ";
                cmd.CommandText += " WHERE LUIS_INTENT = @luisIntent AND MissingEntities is NULL                        ";

                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                Debug.WriteLine("* luisIntent : " + luisIntent);
                Debug.WriteLine("* cmd.CommandText : " + cmd.CommandText);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string ContextLabel = rdr["ContextLabel"] as String;
                    result = ContextLabel;
                }
            }
            return result;
        }

        public String ContextYN(string luisIntent, string conversationId)
        {
            SqlDataReader rdr = null;
            string result = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT INTENT, User_Id, Entities_Values    ";
                cmd.CommandText += " FROM TBL_CONTEXT_LOG                       ";
                cmd.CommandText += " WHERE Intent = @luisIntent                 ";
                cmd.CommandText += " AND User_Id = @conversationId              ";

                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                cmd.Parameters.AddWithValue("@conversationId", conversationId);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string userId = rdr["User_id"] as String;
                    result = userId;
                }
            }
            return result;
        }

        public String ContextEntitiesChk(string luisIntent)
        {
            SqlDataReader rdr = null;
            string result = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT Intent, Entities FROM TBL_CONTEXT_DEFINE    ";
                cmd.CommandText += " WHERE INTENT = @luisIntent                         ";

                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string entities = rdr["Entities"] as String;
                    result = entities;
                }
            }
            return result;
        }

        public String EntitiyDefineChk(string entitiesValue)
        {
            SqlDataReader rdr = null;
            string result = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT ENTITY_VALUE, ENTITY, API_GROUP     ";
                cmd.CommandText += " FROM TBL_COMMON_ENTITY_DEFINE              ";
                cmd.CommandText += " WHERE ENTITY_VALUE = @entitiesValue           ";

                cmd.Parameters.AddWithValue("@entitiesValue", entitiesValue);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string entities = rdr["ENTITY"] as String;
                    result = entities;
                }
            }
            return result;
        }

        public int InsertContextLog(string luisIntent, string conversationId, string contextEntities)
        {
            Debug.WriteLine("luisIntent ::: " + luisIntent);
            Debug.WriteLine("conversationId ::: " + conversationId);
            Debug.WriteLine("contextEntities ::: " + contextEntities);
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " INSERT INTO TBL_CONTEXT_LOG                        ";
                cmd.CommandText += " (INTENT, User_Id, Entities_Values)                 ";
                cmd.CommandText += " VALUES                                             ";
                cmd.CommandText += " (@luisIntent, @conversationId, @contextEntities)   ";

                //cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                cmd.Parameters.AddWithValue("@conversationId", conversationId);
                cmd.Parameters.AddWithValue("@contextEntities", contextEntities);

                dbResult = cmd.ExecuteNonQuery();
                Debug.WriteLine("query : " + cmd.CommandText);
            }
            return dbResult;
        }

        public int UpdateContextLog(string luisIntent, string conversationId, string contextEntities)
        {
            Debug.WriteLine("luisIntent ::: " + luisIntent);
            Debug.WriteLine("conversationId ::: " + conversationId);
            Debug.WriteLine("contextEntities ::: " + contextEntities);
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " UPDATE TBL_CONTEXT_LOG                     ";
                cmd.CommandText += " SET Entities_Values = @contextEntities     ";
                cmd.CommandText += " WHERE INTENT = @luisIntent                 ";
                cmd.CommandText += " AND User_Id = @conversationId              ";

                //cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                cmd.Parameters.AddWithValue("@conversationId", conversationId);
                cmd.Parameters.AddWithValue("@contextEntities", contextEntities);

                dbResult = cmd.ExecuteNonQuery();
                Debug.WriteLine("query : " + cmd.CommandText);
            }
            return dbResult;
        }


        public String SelectContextLog(string luisIntent, string conversationId)
        {
            SqlDataReader rdr = null;
            string result = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT INTENT,User_Id,Entities_Values  ";
                cmd.CommandText += " FROM TBL_CONTEXT_LOG                   ";
                cmd.CommandText += " WHERE INTENT = @luisIntent             ";
                cmd.CommandText += " AND User_Id = @conversationId          ";

                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                cmd.Parameters.AddWithValue("@conversationId", conversationId);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string entitiesValue = rdr["Entities_Values"] as String;
                    result = entitiesValue;
                }
            }
            return result;
        }

        public String SelectMissingEntities(string luisIntent, string luisEntities)
        {
            SqlDataReader rdr = null;
            string result = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, DLG_ID, ContextLabel, MissingEntities   ";
                cmd.CommandText += " FROM TBL_DLG_RELATION_LUIS                                                          ";
                cmd.CommandText += " WHERE LUIS_INTENT = @luisIntent AND LUIS_ENTITIES = @luisEntities                   ";

                cmd.Parameters.AddWithValue("@luisIntent", luisIntent);
                cmd.Parameters.AddWithValue("@luisEntities", luisEntities);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string missingEntities = rdr["MissingEntities"] as String;
                    result = missingEntities;
                }
            }
            return result;
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public List<RelationList> DefineTypeChk(string luisId, string intentId, string entitiesId)
        {
            SqlDataReader rdr = null;
            List<RelationList> result = new List<RelationList>();
            Debug.WriteLine("luisId ::: " + luisId);
            Debug.WriteLine("intentId ::: " + intentId);
            Debug.WriteLine("entitiesId ::: " + entitiesId);
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT A.LUIS_ID, A.LUIS_INTENT, A.LUIS_ENTITIES, ISNULL(A.DLG_ID,0) AS DLG_ID, A.DLG_API_DEFINE, A.API_ID ";
                cmd.CommandText += "  FROM TBL_DLG_RELATION_LUIS A, TBL_DLG B                                                    ";
                cmd.CommandText += " WHERE A.DLG_ID = B.DLG_ID                                               ";
                //cmd.CommandText += " WHERE LUIS_INTENT = @intentId                                                 ";
                cmd.CommandText += "   AND A.LUIS_ENTITIES = @entities                                                ";
                //cmd.CommandText += "   AND LUIS_ID = @luisId                                                        ";

                if (intentId != null)
                {
                    cmd.Parameters.AddWithValue("@intentId", intentId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@intentId", DBNull.Value);
                }

                if (entitiesId != null)
                {
                    cmd.Parameters.AddWithValue("@entities", entitiesId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@entities", DBNull.Value);
                }

                if (luisId != null)
                {
                    cmd.Parameters.AddWithValue("@luisId", luisId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@luisId", DBNull.Value);
                }
                cmd.CommandText += "   ORDER BY B.DLG_ORDER_NO ASC                                               ";



                Debug.WriteLine("query : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rdr.Read())
                {
                    RelationList relationList = new RelationList();
                    relationList.luisId = rdr["LUIS_ID"] as string;
                    relationList.luisIntent = rdr["LUIS_INTENT"] as string;
                    relationList.luisEntities = rdr["LUIS_ENTITIES"] as string;
                    relationList.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    relationList.dlgApiDefine = rdr["DLG_API_DEFINE"] as string;
                    //relationList.apiId = Convert.ToInt32(rdr["API_ID"] ?? 0);
                    relationList.apiId = rdr["API_ID"].Equals(DBNull.Value) ? 0 : Convert.ToInt32(rdr["API_ID"]);
                    //DBNull.Value
                    result.Add(relationList);
                }
            }
            return result;
        }

        public List<RelationList> DefineTypeChkSpare(string intent, string entity)
        {
            SqlDataReader rdr = null;
            List<RelationList> result = new List<RelationList>();
            entity = Regex.Replace(entity, " ", "");
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT  LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, ISNULL(DLG_ID,0) AS DLG_ID, DLG_API_DEFINE, API_ID ";
                cmd.CommandText += "  FROM  TBL_DLG_RELATION_LUIS                                                    ";
                //cmd.CommandText += " WHERE  LUIS_ENTITIES = @entities                                                ";
                cmd.CommandText += " WHERE  LUIS_INTENT = @intent                                                ";
                cmd.CommandText += " AND  LUIS_ENTITIES = @entities                                                ";

                Debug.WriteLine("query : " + cmd.CommandText);
                Debug.WriteLine("entity : " + entity);
                Debug.WriteLine("intent : " + intent);
                cmd.Parameters.AddWithValue("@intent", intent);
                cmd.Parameters.AddWithValue("@entities", entity);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rdr.Read())
                {
                    RelationList relationList = new RelationList();
                    relationList.luisId = rdr["LUIS_ID"] as string;
                    relationList.luisIntent = rdr["LUIS_INTENT"] as string;
                    relationList.luisEntities = rdr["LUIS_ENTITIES"] as string;
                    relationList.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    relationList.dlgApiDefine = rdr["DLG_API_DEFINE"] as string;
                    //relationList.apiId = Convert.ToInt32(rdr["API_ID"] ?? 0);
                    relationList.apiId = rdr["API_ID"].Equals(DBNull.Value) ? 0 : Convert.ToInt32(rdr["API_ID"]);
                    //DBNull.Value
                    result.Add(relationList);
                }
            }
            return result;
        }


        //KSO END

        //TBL_CHATBOT_CONF 정보 가져오기
        //      LUIS_APP_ID	    - 루이스APP_ID
        //      LUIS_TIME_LIMIT - 루이스제한
        //      LUIS_SCORE_LIMIT - 스코어 제한
        //      LUIS_SUBSCRIPTION   - 루이스구독
        //      BOT_NAME        - 봇이름?
        //      BOT_APP_ID      - 봇앱아이디?
        //      BOT_APP_PASSWORD- 봇앱패스워드?
        //      QUOTE           - 견적url
        //      TESTDRIVE       - 시승url
        //      CATALOG         - 카달로그url
        //      DISCOUNT        - 할인url
        //      EVENT           - 이벤트url

        public List<ConfList> SelectConfig()
        //public List<ConfList> SelectConfig(string config_type)
        {
            SqlDataReader rdr = null;
            List<ConfList> conflist = new List<ConfList>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                DButil.HistoryLog("db conn SelectConfig !!");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = " SELECT CNF_TYPE, CNF_NM, CNF_VALUE" +
                                  " FROM TBL_CHATBOT_CONF " +
                                  //" WHERE CNF_TYPE = 'LUIS_APP_ID' " +
                                  " ORDER BY CNF_TYPE DESC, ORDER_NO ASC ";

                Debug.WriteLine("* cmd.CommandText : " + cmd.CommandText);
                //cmd.Parameters.AddWithValue("@config_type", config_type);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string cnfType = rdr["CNF_TYPE"] as string;
                    string cnfNm = rdr["CNF_NM"] as string;
                    string cnfValue = rdr["CNF_VALUE"] as string;

                    ConfList list = new ConfList();

                    list.cnfType = cnfType;
                    list.cnfNm = cnfNm;
                    list.cnfValue = cnfValue;


                    Debug.WriteLine("* cnfNm : " + cnfNm + " || cnfValue : " + cnfValue);
                    DButil.HistoryLog("* cnfNm : " + cnfNm + " || cnfValue : " + cnfValue);
                    conflist.Add(list);
                }
            }
            return conflist;
        }

        public string SelectChgMsg(string oldMsg)
        {
            SqlDataReader rdr = null;
            string newMsg = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "	SELECT FIND.CHG  CHG_WORD FROM(    					    ";
                cmd.CommandText += "	SELECT                                                  ";
                cmd.CommandText += "			CASE WHEN LEN(ORG_WORD) = LEN(@oldMsg)          ";
                cmd.CommandText += "				THEN CHARINDEX(ORG_WORD, @oldMsg)           ";
                cmd.CommandText += "				ELSE 0                                      ";
                cmd.CommandText += "				END AS FIND_NUM,                            ";
                cmd.CommandText += "				REPLACE(@oldMsg, ORG_WORD, CHG_WORD) CHG    ";
                cmd.CommandText += "	  FROM TBL_WORD_CHG_DICT                                ";
                cmd.CommandText += "	  ) FIND                                                ";
                cmd.CommandText += "	  WHERE FIND.FIND_NUM > 0                               ";





                cmd.Parameters.AddWithValue("@oldMsg", oldMsg);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    newMsg = rdr["CHG_WORD"] as string;
                }
            }
            return newMsg;
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Query Analysis
        // Insert user chat message for history and analysis
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public int insertUserQuery()
        {
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                String luisID = "", intentName = "", entities = "", result = "", intentScore = "";

                int appID = 0, luisScore = 0;


                //if(MessagesController.recommendResult != "recommend")
                if (MessagesController.apiFlag != "RECOMMEND")
                {
                    //if (MessagesController.relationList.Equals(null))
                    if (MessagesController.relationList == null)
                    {
                        entities = "None";
                        intentName = "None";
                        luisID = "None";
                        luisScore = 0;
                    }
                    else
                    {

                        if (MessagesController.relationList.Count() > 0)
                        {
                            if (String.IsNullOrEmpty(MessagesController.relationList[0].luisId))
                            {
                                luisID = "None";
                            }
                            else
                            {
                                luisID = MessagesController.relationList[0].luisId;
                            }
                            if (String.IsNullOrEmpty(MessagesController.relationList[0].luisIntent))
                            {
                                intentName = "None";
                            }
                            else
                            {
                                intentName = MessagesController.relationList[0].luisIntent;
                            }
                            if (String.IsNullOrEmpty(MessagesController.relationList[0].luisEntities))
                            {
                                entities = "None";
                            }
                            else
                            {
                                entities = MessagesController.relationList[0].luisEntities;
                            }
                            if (String.IsNullOrEmpty(MessagesController.relationList[0].luisScore.ToString()))
                            {
                                intentScore = "0";
                            }
                            else
                            {
                                intentScore = MessagesController.relationList[0].luisScore.ToString();
                            }
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(MessagesController.cacheList.luisId))
                            {
                                luisID = "None";
                            }
                            else
                            {
                                luisID = MessagesController.cacheList.luisId;
                            }
                            if (String.IsNullOrEmpty(MessagesController.cacheList.luisIntent))
                            {
                                intentName = "None";
                            }
                            else
                            {
                                intentName = MessagesController.cacheList.luisIntent;
                            }
                            if (String.IsNullOrEmpty(MessagesController.cacheList.luisEntities))
                            {
                                entities = "None";
                            }
                            else
                            {
                                entities = MessagesController.cacheList.luisEntities;
                            }
                            if (String.IsNullOrEmpty(MessagesController.cacheList.luisScore))
                            {
                                intentScore = "0";
                            }
                            else
                            {
                                intentScore = MessagesController.cacheList.luisScore;
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(MessagesController.replyresult))
                    {
                        result = "D";
                    }
                    else
                    {
                        result = MessagesController.replyresult;
                    }
                    conn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandText = "sp_insertusehistory4";

                    cmd.CommandType = CommandType.StoredProcedure;


                    //if (result.Equals("S") || result.Equals("D"))
                    //{
                    //    cmd.Parameters.AddWithValue("@Query", "");
                    //    cmd.Parameters.AddWithValue("@intentID", "");
                    //    cmd.Parameters.AddWithValue("@entitiesIDS", "");
                    //    cmd.Parameters.AddWithValue("@intentScore", "");
                    //    cmd.Parameters.AddWithValue("@luisID", "");
                    //    cmd.Parameters.AddWithValue("@result", result);
                    //    cmd.Parameters.AddWithValue("@appID", appID);
                    //}
                    //else
                    //{
                    Debug.WriteLine("DDDDDD : " + Regex.Replace(MessagesController.queryStr, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline).Trim().ToLower());
                    cmd.Parameters.AddWithValue("@Query", Regex.Replace(MessagesController.queryStr, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline).Trim().ToLower());
                    cmd.Parameters.AddWithValue("@intentID", intentName.Trim());
                    cmd.Parameters.AddWithValue("@entitiesIDS", entities.Trim().ToLower());
                    if (result.Equals("D") || result.Equals("S"))
                    {
                        cmd.Parameters.AddWithValue("@intentScore", "0");
                    }
                    else
                    {
                        //if(MessagesController.relationList != null)
                        //{
                        if (MessagesController.relationList.Count > 0 && MessagesController.relationList[0].luisEntities != null)
                        {
                            cmd.Parameters.AddWithValue("@intentScore", MessagesController.relationList[0].luisScore);
                        }
                        //}
                        else
                        {
                            cmd.Parameters.AddWithValue("@intentScore", MessagesController.cacheList.luisScore);
                        }
                    }
                    cmd.Parameters.AddWithValue("@luisID", luisID);
                    cmd.Parameters.AddWithValue("@result", result);
                    cmd.Parameters.AddWithValue("@appID", appID);
                    //}

                    dbResult = cmd.ExecuteNonQuery();
                }


            }
            return dbResult;
        }

        public int insertUserQuery(string korQuery, string intentID, string entitiesIDS, string intentScore, String luisID, char result, int appID)
        {
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "sp_insertusehistory4";

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Query", korQuery.Trim().ToLower());
                cmd.Parameters.AddWithValue("@intentID", intentID.Trim());
                cmd.Parameters.AddWithValue("@entitiesIDS", entitiesIDS.Trim().ToLower());
                cmd.Parameters.AddWithValue("@intentScore", intentScore.Trim().ToLower());
                cmd.Parameters.AddWithValue("@luisID", luisID);
                cmd.Parameters.AddWithValue("@result", result);
                cmd.Parameters.AddWithValue("@appID", appID);


                dbResult = cmd.ExecuteNonQuery();
            }
            return dbResult;
        }



        public int insertHistory(string userNumber, string channel, int responseTime)
        {
            //SqlDataReader rdr = null;
            int appID = 0;
            int result;
            String intentName = "";

            //if (MessagesController.relationList.Equals(null))
            if (MessagesController.relationList == null)
            {
                intentName = "None";
            }
            else
            {
                if (MessagesController.relationList.Count() > 0)
                {
                    if (String.IsNullOrEmpty(MessagesController.relationList[0].luisIntent))
                    {
                        intentName = "None";
                    }
                    else
                    {
                        intentName = MessagesController.relationList[0].luisIntent;
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(MessagesController.cacheList.luisIntent))
                    {
                        intentName = "None";
                    }
                    else
                    {
                        intentName = MessagesController.cacheList.luisIntent;
                    }
                }
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " INSERT INTO TBL_HISTORY_QUERY ";
                cmd.CommandText += " (USER_NUMBER, CUSTOMER_COMMENT_KR, CHATBOT_COMMENT_CODE, CHANNEL, RESPONSE_TIME, REG_DATE, ACTIVE_FLAG, APP_ID) ";
                cmd.CommandText += " VALUES ";
                cmd.CommandText += " (@userNumber, @customerCommentKR, @chatbotCommentCode, @channel, @responseTime, CONVERT(VARCHAR,  GETDATE(), 101) + ' ' + CONVERT(VARCHAR,  DATEADD( HH, 9, GETDATE() ), 24), 0, @appID) ";

                cmd.Parameters.AddWithValue("@userNumber", userNumber);
                cmd.Parameters.AddWithValue("@customerCommentKR", MessagesController.queryStr);

                if (MessagesController.replyresult.Equals("S"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "SEARCH");
                }
                else if (MessagesController.replyresult.Equals("D"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "ERROR");
                }
                else
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", intentName);
                }

                cmd.Parameters.AddWithValue("@channel", channel);
                cmd.Parameters.AddWithValue("@responseTime", responseTime);
                cmd.Parameters.AddWithValue("@appID", appID);

                result = cmd.ExecuteNonQuery();
                Debug.WriteLine("result : " + result);
            }
            return result;
        }

        public int SelectUserQueryErrorMessageCheck(string userID, int appID)
        {
            SqlDataReader rdr = null;
            int result = 0;
            //userID = arg.Replace("'", "''");
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += " SELECT TOP 1 A.CHATBOT_COMMENT_CODE ";
                cmd.CommandText += " FROM ( ";
                cmd.CommandText += " 	SELECT  ";
                cmd.CommandText += " 		SID, ";
                cmd.CommandText += " 		CASE  CHATBOT_COMMENT_CODE  ";
                cmd.CommandText += " 			WHEN 'SEARCH' THEN '1' ";
                cmd.CommandText += " 			WHEN 'ERROR' THEN '1' ";
                cmd.CommandText += " 			ELSE '0' ";
                cmd.CommandText += " 		END CHATBOT_COMMENT_CODE ";
                cmd.CommandText += " 	FROM TBL_HISTORY_QUERY WHERE USER_NUMBER = '" + userID + "' AND APP_ID = " + appID;
                cmd.CommandText += " ) A ";
                cmd.CommandText += " ORDER BY A.SID DESC ";

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    result = Convert.ToInt32(rdr["CHATBOT_COMMENT_CODE"]);
                }
            }
            return result;
        }



        public string SelectArray(string entities)
        {
            SqlDataReader rdr = null;
            string newMsg = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "	SELECT ";
                cmd.CommandText += "        ISNULL(MAX(CASE WHEN POS = 1 THEN VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 2 THEN ',' + VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 3 THEN ',' + VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 4 THEN ',' + VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 5 THEN ',' + VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 6 THEN ',' + VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 7 THEN ',' + VAL1 END), '') ";
                cmd.CommandText += "        + ISNULL(MAX(CASE WHEN POS = 8 THEN ',' + VAL1 END), '') AS VAL ";
                cmd.CommandText += "        FROM ";
                cmd.CommandText += "            ( ";
                cmd.CommandText += "                SELECT VAL1, POS ";
                cmd.CommandText += "                FROM Split2(@entities, ',') ";
                cmd.CommandText += "            ) A                             ";

                cmd.Parameters.AddWithValue("@entities", entities);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    newMsg = rdr["VAL"] as string;
                }
            }
            return newMsg;
        }

        public String SearchCommonEntities
        {
            get
            {
                String query = Regex.Replace(MessagesController.queryStr, @"[^a-zA-Z0-9ㄱ-힣-]", "", RegexOptions.Singleline).Replace(" ", "");
                SqlDataReader rdr = null;
                //List<RecommendConfirm> rc = new List<RecommendConfirm>();
                String entityarr = "";

                using (SqlConnection conn = new SqlConnection(connStr))
                {

                    conn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    //cmd.CommandText += "SELECT  ENTITY_VALUE, ENTITY ";
                    //cmd.CommandText += "FROM    TBL_COMMON_ENTITY_DEFINE ";
                    //cmd.CommandText += "WHERE   CHARINDEX(ENTITY_VALUE,@kr_query) > 0";

                    cmd.CommandText += "SELECT RESULT AS ENTITIES FROM FN_ENTITY_ORDERBY_ADD(@kr_query) ";

                    cmd.Parameters.AddWithValue("@kr_query", query);

                    rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    //int count = 0;
                    try
                    {
                        while (rdr.Read())
                        {
                            entityarr += rdr["ENTITIES"];
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }

                }
                return entityarr;
            }
        }

        public List<DeliveryData> SelectDeliveryData(String deliveryParamList)
        {
            SqlDataReader rdr = null;
            List<DeliveryData> result = new List<DeliveryData>();
            /*
             * Parameter 정리
             * DATA 예)INVOICE_NUM2=1234,CUSTOMER_NAME=전윤아,ADDRESS_OLD=서울특별시 강서구 화곡3동
             * 송장번호일 경우 두개 이므로 INVOICE_NUM2='1234' OR  INVOICE_NUM1 = '1234' 요런 식으로(현재는 INVOICE_NUM2='1234' 이거 하나만임)
             * 금액일 경우 크다 작다 이므로 다시 설정해야 한다
             */
            String[] temp_param_full = null;

            if (deliveryParamList == null || deliveryParamList.Equals(""))
            {

            }
            else
            {
                temp_param_full = deliveryParamList.Split(new string[] { "#" }, StringSplitOptions.None);
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT INVOICE_NUM1, INVOICE_NUM2, DELIVERY_TYPE, PART, CUSTOMER_NAME, ADDRESS_OLD, ADDRESS_NEW, ";
                cmd.CommandText += " PHONE, BOX_TYPE, COMMISSION_PLACE, ETC, CUSTOMER_COMMENT, PAY_TYPE, FEES, QUANTITY, ";
                cmd.CommandText += " BOOK_TYPE, DELIVERY_TIME, DELIVERY_STATUS, STORE_NUM, STORE_NAME, SM_NUM, SM_NAME, ADDRESS_DETAIL ";
                cmd.CommandText += "    FROM TBL_DELIVERY_DATA";
                cmd.CommandText += "    WHERE 1=1";
                if (deliveryParamList == null || deliveryParamList.Equals(""))
                {

                }
                else
                {
                    for (int ii = 0; ii < temp_param_full.Length; ii++)
                    {
                        String[] temp_param = null;
                        temp_param = temp_param_full[ii].Split(new string[] { "," }, StringSplitOptions.None);//사용되는 곳은 없음. 혹시 필요한건가 해서..

                        cmd.CommandText += " AND " + temp_param_full[ii];
                    }
                }


                //cmd.Parameters.AddWithValue("@strTime", strTime);

                Debug.WriteLine("* SelectDeliveryData() CommandText : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    DeliveryData deliveryData = new DeliveryData();
                    deliveryData.invoice_num1 = rdr["INVOICE_NUM1"] as string;
                    deliveryData.invoice_num2 = rdr["INVOICE_NUM2"] as string;
                    deliveryData.delivery_type = rdr["DELIVERY_TYPE"] as string;
                    deliveryData.part = rdr["PART"] as string;
                    deliveryData.customer_name = rdr["CUSTOMER_NAME"] as string;
                    deliveryData.address_old = rdr["ADDRESS_OLD"] as string;
                    deliveryData.address_new = rdr["ADDRESS_NEW"] as string;
                    deliveryData.phone = rdr["PHONE"] as string;
                    deliveryData.box_type = rdr["BOX_TYPE"] as string;
                    deliveryData.commission_place = rdr["COMMISSION_PLACE"] as string;
                    deliveryData.etc = rdr["ETC"] as string;
                    deliveryData.customer_comment = rdr["CUSTOMER_COMMENT"] as string;
                    deliveryData.pay_type = rdr["PAY_TYPE"] as string;
                    deliveryData.fees = rdr["FEES"] as string;
                    deliveryData.quantity = rdr["QUANTITY"] as string;
                    deliveryData.book_type = rdr["BOOK_TYPE"] as string;
                    deliveryData.delivery_time = rdr["DELIVERY_TIME"] as string;
                    deliveryData.delivery_status = rdr["DELIVERY_STATUS"] as string;
                    deliveryData.store_num = rdr["STORE_NUM"] as string;
                    deliveryData.store_name = rdr["STORE_NAME"] as string;
                    deliveryData.sm_num = rdr["SM_NUM"] as string;
                    deliveryData.sm_name = rdr["SM_NAME"] as string;
                    deliveryData.address_detail = rdr["ADDRESS_DETAIL"] as string;

                    result.Add(deliveryData);
                }

                return result;
            }
        }

        //KSO
        public List<DeliveryData> SelectDeliveryData(JArray columnTitle, JArray columnValue, JArray _resultAnswer)
        {
            SqlDataReader rdr = null;
            List<DeliveryData> result = new List<DeliveryData>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT ";
                cmd.CommandText += " INVOICE_NUM1, INVOICE_NUM2, DELIVERY_TYPE, PART, CUSTOMER_NAME, ADDRESS_OLD, ADDRESS_NEW, ";
                cmd.CommandText += " PHONE, BOX_TYPE, COMMISSION_PLACE, ETC, CUSTOMER_COMMENT, PAY_TYPE, FEES, QUANTITY, ";
                cmd.CommandText += " BOOK_TYPE, DELIVERY_TIME, DELIVERY_STATUS, STORE_NUM, STORE_NAME, SM_NUM, SM_NAME, ADDRESS_DETAIL ";
                cmd.CommandText += "    FROM TBL_DELIVERY_DATA";
                cmd.CommandText += "    WHERE 1=1";
                for (int i = 0; i < columnTitle.Count(); i++)
                {
                    Debug.WriteLine("columnTitle : " + columnTitle);
                    if (columnTitle[i].ToString().Equals("address_old") || columnTitle[i].ToString().Equals("address_new"))
                    {
                        cmd.CommandText += " and Replace(" + columnTitle[i].ToString().ToUpper() + ",' ', '') LIKE '%" + columnValue[i].ToString() + "%'";
                    }
                    else
                    {
                        cmd.CommandText += " and " + columnTitle[i] + " = '" + columnValue[i] + "'";
                    }

                }

                Debug.WriteLine("* SelectDeliveryData() CommandText : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    DeliveryData deliveryData = new DeliveryData();
                    deliveryData.invoice_num1 = rdr["INVOICE_NUM1"] as string;
                    deliveryData.invoice_num2 = rdr["INVOICE_NUM2"] as string;
                    deliveryData.delivery_type = rdr["DELIVERY_TYPE"] as string;
                    deliveryData.part = rdr["PART"] as string;
                    deliveryData.customer_name = rdr["CUSTOMER_NAME"] as string;
                    deliveryData.address_old = rdr["ADDRESS_OLD"] as string;
                    deliveryData.address_new = rdr["ADDRESS_NEW"] as string;
                    deliveryData.phone = rdr["PHONE"] as string;
                    deliveryData.box_type = rdr["BOX_TYPE"] as string;
                    deliveryData.commission_place = rdr["COMMISSION_PLACE"] as string;
                    deliveryData.etc = rdr["ETC"] as string;
                    deliveryData.customer_comment = rdr["CUSTOMER_COMMENT"] as string;
                    deliveryData.pay_type = rdr["PAY_TYPE"] as string;
                    deliveryData.fees = rdr["FEES"] as string;
                    deliveryData.quantity = rdr["QUANTITY"] as string;
                    deliveryData.book_type = rdr["BOOK_TYPE"] as string;
                    deliveryData.delivery_time = rdr["DELIVERY_TIME"] as string;
                    deliveryData.delivery_status = rdr["DELIVERY_STATUS"] as string;
                    deliveryData.store_num = rdr["STORE_NUM"] as string;
                    deliveryData.store_name = rdr["STORE_NAME"] as string;
                    deliveryData.sm_num = rdr["SM_NUM"] as string;
                    deliveryData.sm_name = rdr["SM_NAME"] as string;
                    deliveryData.address_detail = rdr["ADDRESS_DETAIL"] as string;

                    result.Add(deliveryData);
                }

                return result;
            }
        }
        /*
         * 등록해줘 일때 update
         * upateColumn : 컬럼이름(ETC, CUSTOMER_COMMENT)
         * updateData : 업데이트 내용
         * paramData : where 조건
         */
        public int UpdateDeliveryData(string etcData, string commentData, string paramData)
        {
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " UPDATE TBL_DELIVERY_DATA                     ";
                cmd.CommandText += " SET ETC = @etcData,  CUSTOMER_COMMENT = @commentData    ";
                cmd.CommandText += " WHERE " + paramData;

                //cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@etcData", etcData);
                cmd.Parameters.AddWithValue("@commentData", commentData);

                Debug.WriteLine("* UpdateDeliveryData() CommandText : " + cmd.CommandText);
                Debug.WriteLine("* UpdateDeliveryData() etcData : " + etcData);
                Debug.WriteLine("* UpdateDeliveryData() commentData : " + commentData);

                dbResult = cmd.ExecuteNonQuery();
                Debug.WriteLine("query : " + cmd.CommandText);
            }
            return dbResult;
        }

        /*
         * 물량정보조회 시에 배달, 집화 건수 보이기
         * */
        public List<DeliveryTypeList> SelectDeliveryTypeList(String deliveryParamList)
        {
            SqlDataReader rdr = null;
            List<DeliveryTypeList> result = new List<DeliveryTypeList>();

            String[] temp_param_full = null;
            if (deliveryParamList == null || deliveryParamList.Equals(""))
            {

            }
            else
            {
                temp_param_full = deliveryParamList.Split(new string[] { "#" }, StringSplitOptions.None);
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT ISNULL(COUNT(DELIVERY_TYPE),0) AS TYPE_COUNT, DELIVERY_TYPE ";
                cmd.CommandText += "    FROM TBL_DELIVERY_DATA";
                cmd.CommandText += "    WHERE 1=1";
                if (deliveryParamList == null || deliveryParamList.Equals(""))
                {

                }
                else
                {
                    for (int ii = 0; ii < temp_param_full.Length; ii++)
                    {
                        cmd.CommandText += " AND " + temp_param_full[ii];
                    }
                }

                cmd.CommandText += "    GROUP BY DELIVERY_TYPE ";

                //cmd.Parameters.AddWithValue("@strTime", strTime);

                Debug.WriteLine("* SelectDeliveryTypeList() CommandText : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    DeliveryTypeList deliveryTypeList = new DeliveryTypeList();
                    deliveryTypeList.type_count = Convert.ToInt32(rdr["TYPE_COUNT"]);
                    deliveryTypeList.delivery_type = rdr["DELIVERY_TYPE"] as string;

                    result.Add(deliveryTypeList);
                }

                return result;
            }
        }

        //KSO
        public List<HistoryList> OldMentChk(string userId)
        {
            SqlDataReader rdr = null;
            List<HistoryList> oldMsg = new List<HistoryList>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "	SELECT TOP 5                                                                        ";
                cmd.CommandText += "           USER_NUMBER, CUSTOMER_COMMENT_KR, CHATBOT_COMMENT_CODE, CHANNEL, REG_DATE    ";
                cmd.CommandText += "      FROM TBL_HISTORY_QUERY                                                            ";
                cmd.CommandText += "     WHERE 1 = 1                                                                        ";
                cmd.CommandText += "       AND USER_NUMBER = @userNumber                                                    ";
                cmd.CommandText += "  ORDER BY REG_DATE DESC                                                                ";

                cmd.Parameters.AddWithValue("@userNumber", userId);
                Debug.WriteLine("* history CommandText : " + cmd.CommandText);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    HistoryList historyList = new HistoryList();
                    historyList.customer_comment_kr = rdr["CUSTOMER_COMMENT_KR"] as string;

                    oldMsg.Add(historyList);
                }

            }
            return oldMsg;
        }

        //KSO
        public List<DeliveryData> InvoiceNumDeliveryData(String invoiceNum)
        {
            SqlDataReader rdr = null;
            List<DeliveryData> result = new List<DeliveryData>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT ";
                cmd.CommandText += " INVOICE_NUM1, INVOICE_NUM2, DELIVERY_TYPE, PART, CUSTOMER_NAME, ADDRESS_OLD, ADDRESS_NEW, ";
                cmd.CommandText += " PHONE, BOX_TYPE, COMMISSION_PLACE, ETC, CUSTOMER_COMMENT, PAY_TYPE, FEES, QUANTITY, ";
                cmd.CommandText += " BOOK_TYPE, DELIVERY_TIME, DELIVERY_STATUS, STORE_NUM, STORE_NAME, SM_NUM, SM_NAME, ADDRESS_DETAIL ";
                cmd.CommandText += "    FROM TBL_DELIVERY_DATA";
                cmd.CommandText += "    WHERE 1=1";
                cmd.CommandText += "    AND INVOICE_NUM2 = @invoiceNum   ";

                cmd.Parameters.AddWithValue("@invoiceNum", invoiceNum);
                Debug.WriteLine("* SelectDeliveryData() CommandText : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    DeliveryData deliveryData = new DeliveryData();
                    deliveryData.invoice_num1 = rdr["INVOICE_NUM1"] as string;
                    deliveryData.invoice_num2 = rdr["INVOICE_NUM2"] as string;
                    deliveryData.delivery_type = rdr["DELIVERY_TYPE"] as string;
                    deliveryData.part = rdr["PART"] as string;
                    deliveryData.customer_name = rdr["CUSTOMER_NAME"] as string;
                    deliveryData.address_old = rdr["ADDRESS_OLD"] as string;
                    deliveryData.address_new = rdr["ADDRESS_NEW"] as string;
                    deliveryData.phone = rdr["PHONE"] as string;
                    deliveryData.box_type = rdr["BOX_TYPE"] as string;
                    deliveryData.commission_place = rdr["COMMISSION_PLACE"] as string;
                    deliveryData.etc = rdr["ETC"] as string;
                    deliveryData.customer_comment = rdr["CUSTOMER_COMMENT"] as string;
                    deliveryData.pay_type = rdr["PAY_TYPE"] as string;
                    deliveryData.fees = rdr["FEES"] as string;
                    deliveryData.quantity = rdr["QUANTITY"] as string;
                    deliveryData.book_type = rdr["BOOK_TYPE"] as string;
                    deliveryData.delivery_time = rdr["DELIVERY_TIME"] as string;
                    deliveryData.delivery_status = rdr["DELIVERY_STATUS"] as string;
                    deliveryData.store_num = rdr["STORE_NUM"] as string;
                    deliveryData.store_name = rdr["STORE_NAME"] as string;
                    deliveryData.sm_num = rdr["SM_NUM"] as string;
                    deliveryData.sm_name = rdr["SM_NAME"] as string;
                    deliveryData.address_detail = rdr["ADDRESS_DETAIL"] as string;

                    result.Add(deliveryData);
                }

                return result;
            }
        }

        /*
       * 물량정보그룹건수 조회
       * */
        public List<DeliveryTypeList> SelectDeliveryGroupList(String groupByParam, String whereParam)
        {
            SqlDataReader rdr = null;
            List<DeliveryTypeList> result = new List<DeliveryTypeList>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT ISNULL(COUNT(DELIVERY_TYPE),0) AS TYPE_COUNT, " + groupByParam + " AS DELIVERY_TYPE";
                cmd.CommandText += "    FROM TBL_DELIVERY_DATA";
                cmd.CommandText += "    WHERE 1=1";
                if (whereParam == null || whereParam.Equals(""))
                {
                    //nothing
                }
                else
                {
                    cmd.CommandText += "    AND " + whereParam;
                }
                cmd.CommandText += "    GROUP BY " + groupByParam;

                //cmd.Parameters.AddWithValue("@strTime", strTime);

                Debug.WriteLine("* SelectDeliveryTypeList() CommandText : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                String nullCheck = "";
                while (rdr.Read())
                {
                    DeliveryTypeList deliveryTypeList = new DeliveryTypeList();
                    nullCheck = rdr["DELIVERY_TYPE"] as string;

                    if (nullCheck == null || nullCheck.Equals(""))
                    {

                    }
                    else
                    {

                        deliveryTypeList.type_count = Convert.ToInt32(rdr["TYPE_COUNT"]);
                        deliveryTypeList.delivery_type = rdr["DELIVERY_TYPE"] as string;
                        result.Add(deliveryTypeList);
                    }



                }

                return result;
            }
        }
    }


}
