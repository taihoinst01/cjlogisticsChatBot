﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using cjlogisticsChatBot.DB;
using cjlogisticsChatBot.Models;
using Newtonsoft.Json.Linq;

using System.Configuration;
using System.Web.Configuration;
using cjlogisticsChatBot.Dialogs;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.ConnectorEx;

namespace cjlogisticsChatBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //MessagesController
        public static readonly string TEXTDLG = "2";
        public static readonly string CARDDLG = "3";
        public static readonly string MEDIADLG = "4";
        public static readonly int MAXFACEBOOKCARDS = 10;

        public static Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/");
        const string chatBotAppID = "appID";
        public static int appID = Convert.ToInt32(rootWebConfig.ConnectionStrings.ConnectionStrings[chatBotAppID].ToString());

        //config 변수 선언
        static public string[] LUIS_NM = new string[10];        //루이스 이름
        static public string[] LUIS_APP_ID = new string[10];    //루이스 app_id
        static public string LUIS_SUBSCRIPTION = "";            //루이스 구독키
        static public int LUIS_TIME_LIMIT;                      //루이스 타임 체크
        static public string QUOTE = "";                        //견적 url
        static public string TESTDRIVE = "";                    //시승 url
        static public string BOT_ID = "";                       //bot id
        static public string MicrosoftAppId = "";               //app id
        static public string MicrosoftAppPassword = "";         //app password
        static public string LUIS_SCORE_LIMIT = "";             //루이스 점수 체크

        public static int sorryMessageCnt = 0;
        public static int chatBotID = 0;

        public static int pagePerCardCnt = 10;
        public static int pageRotationCnt = 0;
        public static string FB_BEFORE_MENT = "";

        public static List<DeliveryData> deliveryData = new List<DeliveryData>();
        public static List<RelationList> relationList = new List<RelationList>();
        public static string luisId = "";
        public static string luisIntent = "";
        public static string luisEntities = "";
        public static string queryStr = "";
        public static DateTime startTime;

        public static CacheList cacheList = new CacheList();
        //페이스북 페이지용
        public static ConversationHistory conversationhistory = new ConversationHistory();
        //추천 컨텍스트 분석용
        public static Dictionary<String, String> recommenddic = new Dictionary<string, String>();
        //결과 플레그 H : 정상 답변, S : 기사검색 답변, D : 답변 실패
        public static String replyresult = "";
        //API 플레그 QUOT : 견적, TESTDRIVE : 시승 RECOMMEND : 추천 COMMON : 일반 SEARCH : 검색
        public static String apiFlag = "";
        public static String recommendResult = "";

        public static string channelID = "";

        public static DbConnect db = new DbConnect();
        public static DButil dbutil = new DButil();

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            string cashOrgMent = "";

            //DbConnect db = new DbConnect();
            //DButil dbutil = new DButil();
            DButil.HistoryLog("db connect !! ");
            //HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            HttpResponseMessage response;

            Activity reply1 = activity.CreateReply();
            Activity reply2 = activity.CreateReply();
            Activity reply3 = activity.CreateReply();
            Activity reply4 = activity.CreateReply();

            // Activity 값 유무 확인하는 익명 메소드
            Action<Activity> SetActivity = (act) =>
            {
                if (!(reply1.Attachments.Count != 0 || reply1.Text != ""))
                {
                    reply1 = act;
                }
                else if (!(reply2.Attachments.Count != 0 || reply2.Text != ""))
                {
                    reply2 = act;
                }
                else if (!(reply3.Attachments.Count != 0 || reply3.Text != ""))
                {
                    reply3 = act;
                }
                else if (!(reply4.Attachments.Count != 0 || reply4.Text != ""))
                {
                    reply4 = act;
                }
                else
                {

                }
            };

            if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                startTime = DateTime.Now;
                //activity.ChannelId = "facebook";
                //파라메터 호출
                if (LUIS_NM.Count(s => s != null) > 0)
                {
                    //string[] LUIS_NM = new string[10];
                    Array.Clear(LUIS_NM, 0, LUIS_NM.Length);
                }

                if (LUIS_APP_ID.Count(s => s != null) > 0)
                {
                    //string[] LUIS_APP_ID = new string[10];
                    Array.Clear(LUIS_APP_ID, 0, LUIS_APP_ID.Length);
                }
                //Array.Clear(LUIS_APP_ID, 0, 10);
                DButil.HistoryLog("db SelectConfig start !! ");
                List<ConfList> confList = db.SelectConfig();
                DButil.HistoryLog("db SelectConfig end!! ");

                for (int i = 0; i < confList.Count; i++)
                {
                    switch (confList[i].cnfType)
                    {
                        case "LUIS_APP_ID":
                            LUIS_APP_ID[LUIS_APP_ID.Count(s => s != null)] = confList[i].cnfValue;
                            LUIS_NM[LUIS_NM.Count(s => s != null)] = confList[i].cnfNm;
                            break;
                        case "LUIS_SUBSCRIPTION":
                            LUIS_SUBSCRIPTION = confList[i].cnfValue;
                            break;
                        case "BOT_ID":
                            BOT_ID = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppId":
                            MicrosoftAppId = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppPassword":
                            MicrosoftAppPassword = confList[i].cnfValue;
                            break;
                        case "QUOTE":
                            QUOTE = confList[i].cnfValue;
                            break;
                        case "TESTDRIVE":
                            TESTDRIVE = confList[i].cnfValue;
                            break;
                        case "LUIS_SCORE_LIMIT":
                            LUIS_SCORE_LIMIT = confList[i].cnfValue;
                            break;
                        case "LUIS_TIME_LIMIT":
                            LUIS_TIME_LIMIT = Convert.ToInt32(confList[i].cnfValue);
                            break;
                        default: //미 정의 레코드
                            Debug.WriteLine("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            DButil.HistoryLog("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            break;
                    }
                }

                Debug.WriteLine("* DB conn : " + activity.Type);
                DButil.HistoryLog("* DB conn : " + activity.Type);

                //초기 다이얼로그 호출
                List<DialogList> dlg = db.SelectInitDialog(activity.ChannelId);

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                foreach (DialogList dialogs in dlg)
                {
                    Activity initReply = activity.CreateReply();
                    initReply.Recipient = activity.From;
                    initReply.Type = "message";
                    initReply.Attachments = new List<Attachment>();
                    //initReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    Attachment tempAttachment;

                    if (dialogs.dlgType.Equals(CARDDLG))
                    {
                        foreach (CardList tempcard in dialogs.dialogCard)
                        {
                            tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                            initReply.Attachments.Add(tempAttachment);
                        }
                    }
                    else
                    {
                        if (activity.ChannelId.Equals("facebook") && string.IsNullOrEmpty(dialogs.cardTitle) && dialogs.dlgType.Equals(TEXTDLG))
                        {
                            Activity reply_facebook = activity.CreateReply();
                            reply_facebook.Recipient = activity.From;
                            reply_facebook.Type = "message";
                            DButil.HistoryLog("facebook  card Text : " + dialogs.cardText);
                            reply_facebook.Text = dialogs.cardText;
                            var reply_ment_facebook = connector.Conversations.SendToConversationAsync(reply_facebook);
                            //SetActivity(reply_facebook);

                        }
                        else
                        {
                            tempAttachment = dbutil.getAttachmentFromDialog(dialogs, activity);
                            initReply.Attachments.Add(tempAttachment);
                        }
                    }
                    await connector.Conversations.SendToConversationAsync(initReply);
                }

                DateTime endTime = DateTime.Now;
                Debug.WriteLine("프로그램 수행시간 : {0}/ms", ((endTime - startTime).Milliseconds));
                Debug.WriteLine("* activity.Type : " + activity.Type);
                Debug.WriteLine("* activity.Recipient.Id : " + activity.Recipient.Id);
                Debug.WriteLine("* activity.ServiceUrl : " + activity.ServiceUrl);

                DButil.HistoryLog("* activity.Type : " + activity.ChannelData);
                DButil.HistoryLog("* activity.Recipient.Id : " + activity.Recipient.Id);
                DButil.HistoryLog("* activity.ServiceUrl : " + activity.ServiceUrl);
            }
            else if (activity.Type == ActivityTypes.Message)
            {
                //activity.ChannelId = "facebook";
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                try
                {
                    Debug.WriteLine("* activity.Type == ActivityTypes.Message ");
                    channelID = activity.ChannelId;
                    string orgMent = activity.Text;


                    apiFlag = "COMMON";

                    //대화 시작 시간
                    startTime = DateTime.Now;
                    long unixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                    DButil.HistoryLog("orgMent : " + orgMent);
                    //금칙어 체크
                    CardList bannedMsg = db.BannedChk(orgMent);
                    Debug.WriteLine("* bannedMsg : " + bannedMsg.cardText);//해당금칙어에 대한 답변

                    if (bannedMsg.cardText != null)
                    {
                        Activity reply_ment = activity.CreateReply();
                        reply_ment.Recipient = activity.From;
                        reply_ment.Type = "message";
                        reply_ment.Text = bannedMsg.cardText;

                        var reply_ment_info = await connector.Conversations.SendToConversationAsync(reply_ment);
                        response = Request.CreateResponse(HttpStatusCode.OK);
                        return response;
                    }
                    else
                    {
                        queryStr = orgMent;
                        //인텐트 엔티티 검출
                        //캐시 체크
                        cashOrgMent = Regex.Replace(orgMent, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                        //cacheList = db.CacheChk(cashOrgMent.Replace(" ", ""));                     // 캐시 체크

                        JArray compositEntities = new JArray();
                        JArray entities = new JArray();

                        //캐시에 없을 경우
                        if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                        {
                            DButil.HistoryLog("cache none : " + orgMent);
                            //루이스 체크
                            cacheList.luisId = dbutil.GetMultiLUIS(orgMent);

                            compositEntities = dbutil.GetCompositEnities(orgMent);  //compositEntities 가져오는 부분
                            entities = dbutil.GetEnities(orgMent);  //entities 가져오는 부분
                        }

                        ////////////////////////////

                        //Intent로 context모듈인지 select (T,F)
                        var contextChk = db.ContextChk(cacheList.luisIntent);
                        DButil.HistoryLog("contextChk : " + contextChk);

                        if (contextChk.Equals("T"))
                        {
                            // 새로운 사용자인지 조회
                            var contextYN = db.ContextYN(cacheList.luisIntent, activity.Conversation.Id);
                            //intent로 contextType 가져오기
                            var selectEntities = db.ContextEntitiesChk(cacheList.luisIntent);
                            //var entitiesValue = cacheList.luisEntities.Split(',');  //user value
                            var contextEntitiesValue = selectEntities.Split(',');   //default value
                            var insertEntities = "";
                            var updateEntities = "";

                            //DButil.HistoryLog("contextYN : " + contextYN);
                            //DButil.HistoryLog("cacheList.luisIntent : " + cacheList.luisIntent);
                            //DButil.HistoryLog("selectEntities : " + selectEntities);

                            //조회되지 않을경우 새로운 사용자로 판단되어 contextLog를 쌓는다.
                            if (contextYN.Equals(""))   // 새로운 사용자
                            {
                                //DButil.HistoryLog("contextEntitiesValue.Length : " + contextEntitiesValue.Length);
                                if (compositEntities.Count() > 0)
                                {
                                    //compositEntities 있을경우
                                    for (var i = 0; i < contextEntitiesValue.Length; i++)
                                    {
                                        //DButil.HistoryLog("contextEntitiesValue : " + i + " : " + contextEntitiesValue[i]);
                                        insertEntities = insertEntities + contextEntitiesValue[i] + ":";
                                        for (int j = 0; j < compositEntities.Count(); j++)
                                        {
                                            //DButil.HistoryLog("contextEntitiesValue : " + j + " : " + compositEntities[j]);
                                            if ((j % 2).Equals(1) && "숫자".Equals(compositEntities[j]["type"].ToString()))
                                            {
                                                if (contextEntitiesValue[i].Equals(compositEntities[j - 1]["type"].ToString()))
                                                {
                                                    insertEntities = insertEntities + compositEntities[j]["value"].ToString();
                                                }
                                            }
                                            else
                                            {
                                                if (j > 0 && contextEntitiesValue[i].Equals(compositEntities[j]["type"].ToString()))
                                                {
                                                    if ("숫자".Equals(compositEntities[j - 1]["type"].ToString()))
                                                    {
                                                        insertEntities = insertEntities + compositEntities[j - 1]["value"].ToString();
                                                    }
                                                }
                                            }
                                        }
                                        insertEntities = insertEntities + ",";
                                    }
                                }
                                else
                                {
                                    //compositEntities 없어서 Entites로만
                                    DButil.HistoryLog("no!!! ");
                                    for (var i = 0; i < contextEntitiesValue.Length; i++)
                                    {
                                        insertEntities = insertEntities + contextEntitiesValue[i] + ":";
                                        for (var j = 0; j < entities.Count(); j++)
                                        {
                                            //DButil.HistoryLog("contextEntitiesValue[i] :::::::: " + contextEntitiesValue[i]);
                                            //DButil.HistoryLog("entities :::::::: " + entities[j]["type"].ToString());
                                            if (contextEntitiesValue[i].Equals(entities[j]["type"].ToString()))
                                            {
                                                insertEntities = insertEntities + entities[j]["entity"].ToString().Replace(" ", "");
                                            }
                                        }
                                        insertEntities = insertEntities + ",";
                                    }
                                }

                                insertEntities = insertEntities.Substring(0, insertEntities.Length - 1);
                                DButil.HistoryLog("insertEntities : " + insertEntities);

                                //새로운 사용자일 경우 insert
                                db.InsertContextLog(cacheList.luisIntent, activity.Conversation.Id, insertEntities);
                            }
                            else
                            {
                                // 기존 사용자 업데이트
                                if (compositEntities.Count() > 0)
                                {
                                    //compositEntities 있을경우
                                    //DButil.HistoryLog("contextEntitiesValue.Length : " + contextEntitiesValue.Length);
                                    for (var i = 0; i < contextEntitiesValue.Length; i++)
                                    {
                                        //DButil.HistoryLog("contextEntitiesValue : " + i + " : " + contextEntitiesValue[i]);
                                        updateEntities = updateEntities + contextEntitiesValue[i] + ":";
                                        for (int j = 0; j < compositEntities.Count(); j++)
                                        {
                                            //DButil.HistoryLog("contextEntitiesValue : " + j + " : " + compositEntities[j]);
                                            if ((j % 2).Equals(1) && "숫자".Equals(compositEntities[j]["type"].ToString()))
                                            {
                                                if (contextEntitiesValue[i].Equals(compositEntities[j - 1]["type"].ToString()))
                                                {
                                                    updateEntities = updateEntities + compositEntities[j]["value"].ToString();
                                                }
                                            }
                                            else
                                            {
                                                if (j > 0 && contextEntitiesValue[i].Equals(compositEntities[j]["type"].ToString()))
                                                {
                                                    if ("숫자".Equals(compositEntities[j - 1]["type"].ToString()))
                                                    {
                                                        updateEntities = updateEntities + compositEntities[j - 1]["value"].ToString();
                                                    }
                                                }
                                            }
                                        }
                                        updateEntities = updateEntities + ",";
                                    }
                                }
                                else
                                {
                                    //compositEntities 없어서 Entites로만
                                    for (var i = 0; i < contextEntitiesValue.Length; i++)
                                    {
                                        updateEntities = updateEntities + contextEntitiesValue[i] + ":";
                                        for (var j = 0; j < entities.Count(); j++)
                                        {
                                            //DButil.HistoryLog("contextEntitiesValue[i] :::::::: " + contextEntitiesValue[i]);
                                            //DButil.HistoryLog("entities :::::::: " + entities[j]["type"].ToString());
                                            if (contextEntitiesValue[i].Equals(entities[j]["type"].ToString()))
                                            {
                                                updateEntities = updateEntities + entities[j]["entity"].ToString().Replace(" ", "");
                                            }
                                        }
                                        updateEntities = updateEntities + ",";
                                    }
                                }

                                

                                updateEntities = updateEntities.Substring(0, updateEntities.Length - 1);
                                DButil.HistoryLog("updateEntities : " + updateEntities);

                                //기존의 사용자일 경우 update
                                db.UpdateContextLog(cacheList.luisIntent, activity.Conversation.Id, updateEntities);
                            }
                        }


                        ////////////////////////////


                        luisId = cacheList.luisId;
                        luisIntent = cacheList.luisIntent;
                        luisEntities = cacheList.luisEntities;

                        DButil.HistoryLog("luisId : " + luisId);
                        DButil.HistoryLog("luisIntent : " + luisIntent);
                        DButil.HistoryLog("luisEntities : " + luisEntities);


                        String fullentity = db.SearchCommonEntities;
                        DButil.HistoryLog("fullentity : " + fullentity);
                        if (!string.IsNullOrEmpty(fullentity) || !fullentity.Equals(""))
                        {
                            if (!String.IsNullOrEmpty(luisEntities))
                            {
                                //entity 길이 비교
                                if (fullentity.Length > luisEntities.Length || luisIntent == null || luisIntent.Equals(""))
                                {
                                    //DefineTypeChkSpare에서는 인텐트나 루이스아이디조건 없이 엔티티만 일치하면 다이얼로그 리턴
                                    relationList = db.DefineTypeChkSpare(fullentity);
                                }
                                else
                                {
                                    relationList = db.DefineTypeChk(MessagesController.luisId, MessagesController.luisIntent, MessagesController.luisEntities);
                                }
                            }
                            else
                            {
                                relationList = db.DefineTypeChkSpare(fullentity);
                            }
                        }
                        else
                        {

                            if (apiFlag.Equals("COMMON"))
                            {
                                relationList = db.DefineTypeChkSpare(cacheList.luisEntities);
                            }
                            else
                            {
                                relationList = null;
                            }

                        }
                        if (relationList != null)
                        //if (relationList.Count > 0)
                        {
                            DButil.HistoryLog("relationList 조건 in ");
                            if (relationList.Count > 0 && relationList[0].dlgApiDefine != null)
                            {
                                if (relationList[0].dlgApiDefine.Equals("api testdrive"))
                                {
                                    apiFlag = "TESTDRIVE";
                                }
                                else if (relationList[0].dlgApiDefine.Equals("api quot"))
                                {
                                    apiFlag = "QUOT";
                                }
                                else if (relationList[0].dlgApiDefine.Equals("api recommend"))
                                {
                                    apiFlag = "RECOMMEND";
                                }
                                else if (relationList[0].dlgApiDefine.Equals("D"))
                                {
                                    apiFlag = "COMMON";
                                }
                                DButil.HistoryLog("relationList[0].dlgApiDefine : " + relationList[0].dlgApiDefine);
                            }

                        }
                        else
                        {

                            if (MessagesController.cacheList.luisIntent == null || apiFlag.Equals("COMMON"))
                            {
                                apiFlag = "";
                            }
                            else if (MessagesController.cacheList.luisId.Equals("cjlogisticsChatBot_luis_01") && MessagesController.cacheList.luisIntent.Contains("quot"))
                            {
                                apiFlag = "QUOT";
                            }
                            DButil.HistoryLog("apiFlag : " + apiFlag);
                        }


                        if (apiFlag.Equals("COMMON") && relationList.Count > 0)
                        {

                            //context.Call(new CommonDialog("", MessagesController.queryStr), this.ResumeAfterOptionDialog);

                            for (int m = 0; m < MessagesController.relationList.Count; m++)
                            {
                                DialogList dlg = db.SelectDialog(MessagesController.relationList[m].dlgId);
                                Activity commonReply = activity.CreateReply();
                                Attachment tempAttachment = new Attachment();
                                DButil.HistoryLog("dlg.dlgType : " + dlg.dlgType);
                                if (dlg.dlgType.Equals(CARDDLG))
                                {
                                    foreach (CardList tempcard in dlg.dialogCard)
                                    {
                                        DButil.HistoryLog("tempcard.card_order_no : " + tempcard.card_order_no);

                                        tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                        if (tempAttachment != null)
                                        {
                                            commonReply.Attachments.Add(tempAttachment);
                                        }

                                        //2018-04-19:KSO:Carousel 만드는부분 추가
                                        if (tempcard.card_order_no > 1)
                                        {
                                            commonReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        }

                                    }
                                }
                                else
                                {
                                   /*
                                    * 답변 하는 부분
                                    * cj 대한통운
                                    * 2018.06.26 JunHyoung Park
                                    */
                                    String param_intent = "물량정보";
                                    String param_entities = "DELIVERY_STATUS='집화완료', PAY_TYPE='착불'";
                                    deliveryData = db.SelectDeliveryData(param_entities);
                                    String deliveryDataText = "";
                                    string strComment = null;
                                    int deliveryDataCount = 0;
                                    /*
                                     * parameter 정리
                                     */
                                    String invoice_num1 = null;
                                    String invoice_num2 = null;
                                    String delivery_type = null;
                                    String part = null;
                                    String customer_name = null;
                                    String address_old = null;
                                    String address_new = null;
                                    String phone = null;
                                    String box_type = null;
                                    String commission_place = null;
                                    String etc = null;
                                    String customer_comment = null;
                                    String pay_type = null;
                                    String fees = null;
                                    String quantity = null;
                                    String book_type = null;
                                    String delivery_time = null;
                                    String delivery_status = null;
                                    String store_num = null;
                                    String store_name = null;
                                    String sm_num = null;
                                    String sm_name = null;
                                    String separate_line = "\n\n";

                                    if (deliveryData != null)
                                    {
                                        deliveryDataCount = deliveryData.Count;
                                        if (deliveryData.Count == 1)
                                        {
                                            invoice_num1 = deliveryData[0].invoice_num1 + "\n";
                                            invoice_num2 = deliveryData[0].invoice_num2 + "\n";
                                            delivery_type = deliveryData[0].delivery_type + "\n";
                                            part = deliveryData[0].part + "\n";
                                            customer_name = deliveryData[0].customer_name + "\n";
                                            address_old = deliveryData[0].address_old + "\n";
                                            address_new = deliveryData[0].address_new + "\n";
                                            phone = deliveryData[0].phone + "\n";
                                            box_type = deliveryData[0].box_type + "\n";
                                            commission_place = deliveryData[0].commission_place + "\n";
                                            etc = deliveryData[0].etc + "\n";
                                            customer_comment = deliveryData[0].customer_comment + "\n";
                                            pay_type = deliveryData[0].pay_type + "\n";
                                            fees = deliveryData[0].fees + "\n";
                                            quantity = deliveryData[0].quantity + "\n";
                                            book_type = deliveryData[0].book_type + "\n";
                                            delivery_time = deliveryData[0].delivery_time + "\n";
                                            delivery_status = deliveryData[0].delivery_status + "\n";
                                            store_num = deliveryData[0].store_num + "\n";
                                            store_name = deliveryData[0].store_name + "\n";
                                            sm_num = deliveryData[0].sm_num + "\n";
                                            sm_name = deliveryData[0].sm_name + "\n";

                                            dlg.cardText = dlg.cardText.Replace("#invoice_num1", invoice_num1);
                                            dlg.cardText = dlg.cardText.Replace("#invoice_num2", invoice_num2);
                                            dlg.cardText = dlg.cardText.Replace("#delivery_type", delivery_type);
                                            dlg.cardText = dlg.cardText.Replace("#part", part);
                                            dlg.cardText = dlg.cardText.Replace("#customer_name", customer_name);
                                            dlg.cardText = dlg.cardText.Replace("#address_old", address_old);
                                            dlg.cardText = dlg.cardText.Replace("#address_new", address_new);
                                            dlg.cardText = dlg.cardText.Replace("#phone", phone);
                                            dlg.cardText = dlg.cardText.Replace("#box_type", box_type);
                                            dlg.cardText = dlg.cardText.Replace("#commission_place", commission_place);
                                            dlg.cardText = dlg.cardText.Replace("#etc", etc);
                                            dlg.cardText = dlg.cardText.Replace("#customer_comment", customer_comment);
                                            dlg.cardText = dlg.cardText.Replace("#pay_type", pay_type);
                                            dlg.cardText = dlg.cardText.Replace("#fees", fees);
                                            dlg.cardText = dlg.cardText.Replace("#quantity", quantity);
                                            dlg.cardText = dlg.cardText.Replace("#book_type", book_type);
                                            dlg.cardText = dlg.cardText.Replace("#delivery_time", delivery_time);
                                            dlg.cardText = dlg.cardText.Replace("#delivery_status", delivery_status);
                                            dlg.cardText = dlg.cardText.Replace("#store_num", store_num);
                                            dlg.cardText = dlg.cardText.Replace("#store_name", store_name);
                                            dlg.cardText = dlg.cardText.Replace("#sm_num", sm_num);
                                            dlg.cardText = dlg.cardText.Replace("#sm_name", sm_name);
                                        }
                                        else
                                        {
                                            /*
                                             * 한개 이상의 데이터가 나오므로 여기는 뭉텡이로
                                             * 보여주어야 할 데이터를 만들어야 겠네요.
                                             */
                                            for (int i = 0; i < deliveryData.Count; i++)
                                            {
                                                strComment = "송장번호 : " + deliveryData[i].invoice_num1 + "\n";
                                                strComment += "송장번호 : " + deliveryData[i].invoice_num2 + "\n";
                                                strComment += "고객명 : " + deliveryData[i].customer_name + "\n";
                                                strComment += separate_line;
                                                deliveryDataText = deliveryDataText + strComment;
                                            }
                                            dlg.cardText = dlg.cardText.Replace("#deliveryData", deliveryDataText);
                                        }
                                    }
                                    else
                                    {
                                        deliveryDataCount = 0;
                                    }

                                    DButil.HistoryLog("facebook dlg.dlgId : " + dlg.dlgId);
                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);

                                }

                                if (commonReply.Attachments.Count > 0)
                                {
                                    SetActivity(commonReply);
                                    conversationhistory.commonBeforeQustion = orgMent;
                                    replyresult = "H";

                                }
                            }
                        }
                        else
                        {
                            string newUserID = activity.Conversation.Id;
                            string beforeUserID = "";
                            string beforeMessgaeText = "";
                            //string messgaeText = "";

                            Activity intentNoneReply = activity.CreateReply();
                            Boolean sorryflag = false;


                            if (beforeUserID != newUserID)
                            {
                                beforeUserID = newUserID;
                                MessagesController.sorryMessageCnt = 0;
                            }

                            var message = MessagesController.queryStr;
                            beforeMessgaeText = message.ToString();

                            Debug.WriteLine("SERARCH MESSAGE : " + message);
                            //네이버 기사 검색
                            if (sorryflag)
                            {
                                //Sorry Message 
                                int sorryMessageCheck = db.SelectUserQueryErrorMessageCheck(activity.Conversation.Id, MessagesController.chatBotID);

                                ++MessagesController.sorryMessageCnt;

                                Activity sorryReply = activity.CreateReply();

                                sorryReply.Recipient = activity.From;
                                sorryReply.Type = "message";
                                sorryReply.Attachments = new List<Attachment>();
                                sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                                List<TextList> text = new List<TextList>();
                                if (sorryMessageCheck == 0)
                                {
                                    text = db.SelectSorryDialogText("5");
                                }
                                else
                                {
                                    text = db.SelectSorryDialogText("6");
                                }

                                for (int i = 0; i < text.Count; i++)
                                {
                                    HeroCard plCard = new HeroCard()
                                    {
                                        Title = text[i].cardTitle,
                                        Text = text[i].cardText
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    sorryReply.Attachments.Add(plAttachment);
                                }

                                SetActivity(sorryReply);
                                //await connector.Conversations.SendToConversationAsync(sorryReply);
                                sorryflag = false;
                                replyresult = "D";
                            }
                        }

                        DateTime endTime = DateTime.Now;
                        //analysis table insert
                        //if (rc != null)
                        //{
                        int dbResult = db.insertUserQuery();

                        //}
                        //history table insert
                        db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds));
                        replyresult = "";
                        recommendResult = "";
                    }
                }
                catch (Exception e)
                {
                    Debug.Print(e.StackTrace);
                    int sorryMessageCheck = db.SelectUserQueryErrorMessageCheck(activity.Conversation.Id, MessagesController.chatBotID);

                    ++MessagesController.sorryMessageCnt;

                    Activity sorryReply = activity.CreateReply();

                    sorryReply.Recipient = activity.From;
                    sorryReply.Type = "message";
                    sorryReply.Attachments = new List<Attachment>();
                    //sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    List<TextList> text = new List<TextList>();
                    if (sorryMessageCheck == 0)
                    {
                        text = db.SelectSorryDialogText("5");
                    }
                    else
                    {
                        text = db.SelectSorryDialogText("6");
                    }

                    for (int i = 0; i < text.Count; i++)
                    {
                        HeroCard plCard = new HeroCard()
                        {
                            Title = text[i].cardTitle,
                            Text = text[i].cardText
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        sorryReply.Attachments.Add(plAttachment);
                    }

                    SetActivity(sorryReply);

                    DateTime endTime = DateTime.Now;
                    int dbResult = db.insertUserQuery();
                    db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds));
                    replyresult = "";
                    recommendResult = "";
                }
                finally
                {
                    if (reply1.Attachments.Count != 0 || reply1.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply1);
                    }
                    if (reply2.Attachments.Count != 0 || reply2.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply2);
                    }
                    if (reply3.Attachments.Count != 0 || reply3.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply3);
                    }
                    if (reply4.Attachments.Count != 0 || reply4.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply4);
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            response = Request.CreateResponse(HttpStatusCode.OK);
            return response;

        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.Typing)
            {
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            return null;
        }

        private static Attachment GetHeroCard_facebookMore(string title, string subtitle, string text, CardAction cardAction)
        {
            var heroCard = new UserHeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Buttons = new List<CardAction>() { cardAction },
            };
            return heroCard.ToAttachment();
        }
    }
}