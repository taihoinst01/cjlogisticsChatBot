using System;
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

                        //루이스 체크
                        cacheList.luisId = dbutil.GetMultiLUIS(orgMent);

                        entities = dbutil.GetEnities(orgMent);  //entities 가져오는 부분

                        Debug.WriteLine("*******************************full entities : " + entities);
                        String temp_entityType = "";
                        for (var j = 0; j < entities.Count(); j++)
                        {
                            temp_entityType = temp_entityType + entities[j]["type"].ToString() + ", ";
                        }
                        temp_entityType = temp_entityType.Substring(0, temp_entityType.Length - 2);
                        Debug.WriteLine("*******************************temp_entityType : " + temp_entityType);
                        cacheList.luisEntities = temp_entityType;


                        luisId = cacheList.luisId;
                        luisIntent = cacheList.luisIntent;
                        luisEntities = cacheList.luisEntities;

                        DButil.HistoryLog("luisId : " + luisId);
                        DButil.HistoryLog("luisIntent : " + luisIntent);
                        DButil.HistoryLog("luisEntities : " + luisEntities);


                        String fullentity = db.SearchCommonEntities;
                        DButil.HistoryLog("fullentity : " + fullentity);
                        if (apiFlag.Equals("COMMON"))
                        {
                            relationList = db.DefineTypeChkSpare(cacheList.luisIntent, cacheList.luisEntities);
                        }
                        else
                        {
                            relationList = null;
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
                                /*
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
                                */
                                   /*
                                    * 답변 하는 부분
                                    * cj 대한통운
                                    * 2018.06.26 JunHyoung Park
                                    */
                                    //String param_intent = "물량정보";
                                    //String param_entities = "DELIVERY_STATUS='집화완료', PAY_TYPE='착불'";


                                    String param_intent = MessagesController.cacheList.luisIntent;
                                    String temp_paramEntities = null;
                                    String[] column_name = new String[] {"invoice_num1", "invoice_num2", "delivery_type", "part", "customer_name", "address_old", "address_new", "phone", "box_type", "commission_place", "etc", "customer_comment", "pay_type", "fees", "quantity", "book_type", "delivery_time", "delivery_status", "store_num", "store_name", "sm_num", "sm_name"};

                                    if (param_intent.Equals("물량정보조회3")){
                                    //강신욱 주임님 작업공간
                                    Debug.WriteLine("param_intent :: " + param_intent);
                                    DButil.HistoryLog("param_intent : " + param_intent);

                                    JArray _columnTitle = new JArray();
                                    JArray _columValue = new JArray();
                                    JArray _resultAnswer = new JArray();

                                    for (int i = 0; i < entities.Count(); i++)
                                    {
                                        var resultAnswerChk = entities[i]["type"].ToString().Substring(0, 2);
                                        if (resultAnswerChk.Equals("r_"))
                                        {   //검색하고자 하는 entity배열
                                            var answerEntity = entities[i]["type"].ToString().Substring(2, entities[i]["type"].ToString().Length - 2);
                                            _resultAnswer.Add(answerEntity);
                                        }
                                        else
                                        {   //조건에 맞는 entity 배열
                                            _columnTitle.Add(entities[i]["type"].ToString());
                                            _columValue.Add(Regex.Replace(entities[i]["entity"].ToString(), " ", ""));
                                        }
                                    }

                                    deliveryData = db.SelectDeliveryData(_columnTitle, _columValue);
                                    if (deliveryData != null)
                                    {
                                        var oriTextData = dlg.cardText;
                                        DButil.HistoryLog("deliveryData.Count ::::: " + deliveryData.Count);
                                        for (int i = 0; i < deliveryData.Count(); i++)
                                        {
                                            //데이터 셋팅
                                            if (i.Equals(0))
                                            {
                                                dlg.cardTitle = dlg.cardTitle + " (총 건수는 : " + deliveryData.Count() + "건 입니다.)";
                                            }
                                            dlg.cardText = dlg.cardText.Replace("##INVOICE_NUM1", deliveryData[i].invoice_num1 + "\n\n");
                                            dlg.cardText = dlg.cardText.Replace("##INVOICE_NUM2", deliveryData[i].invoice_num2 + "\n\n");
                                            dlg.cardText = dlg.cardText.Replace("##DELIVERY_TYPE", deliveryData[i].delivery_type + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##PART", deliveryData[i].part + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##CUSTOMER_NAME", deliveryData[i].customer_name + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##ADDRESS_OLD", deliveryData[i].address_old + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##ADDRESS_NEW", deliveryData[i].address_new + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##PHONE", deliveryData[i].phone + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##BOX_TYPE", deliveryData[i].box_type + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##COMMISSION_PLACE", deliveryData[i].commission_place + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##ETC", deliveryData[i].etc + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##CUSTOMER_COMMENT", deliveryData[i].customer_comment + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##PAY_TYPE", deliveryData[i].pay_type + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##FEES", deliveryData[i].fees + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##QUANTITY", deliveryData[i].quantity + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##BOOK_TYPE", deliveryData[i].book_type + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##DELIVERY_TIME", deliveryData[i].delivery_time + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##DELIVERY_STATUS", deliveryData[i].delivery_status + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##STORE_NUM", deliveryData[i].store_num + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##STORE_NAME", deliveryData[i].store_name + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##SM_NUM", deliveryData[i].sm_num + "\n");
                                            dlg.cardText = dlg.cardText.Replace("##SM_NAME", deliveryData[i].sm_name);

                                            //카드 출력
                                            tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                            commonReply.Attachments.Add(tempAttachment);

                                            //데이터 초기화
                                            if (i.Equals(0))
                                            {
                                                dlg.cardTitle = dlg.cardTitle.Replace(dlg.cardTitle, "정보");
                                            }
                                            dlg.cardText = dlg.cardText.Replace(dlg.cardText, oriTextData);
                                        }

                                        if (deliveryData.Count().Equals(0))
                                        {
                                            dlg.cardTitle = dlg.cardTitle + " (총 건수는 : 0 건 입니다.)";
                                            dlg.cardText = dlg.cardText.Replace(dlg.cardText, "0 건의 정보는 조회되지 않습니다.");

                                            //카드 출력
                                            tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                            commonReply.Attachments.Add(tempAttachment);
                                        }
                                    }

                                }
                                    else if (param_intent.Equals("등록신청"))
                                    {
                                        
                                        String etc_data = "";
                                        String comment_data = "";
                                        int db_update_check = 0;
                                        for (var i = 0; i < entities.Count(); i++)
                                        {
                                            if (entities[i]["type"].ToString().Equals("etc_msg"))
                                            {
                                                etc_data = entities[i]["entity"].ToString();
                                            }

                                            if (entities[i]["type"].ToString().Equals("sms_msg"))
                                            {
                                                comment_data = entities[i]["entity"].ToString();
                                            }

                                            for (int ii = 0; ii < column_name.Length; ii++)
                                            {
                                                if (entities[i]["type"].ToString().Equals(column_name[ii]))
                                                {
                                                    String entity_type = entities[i]["type"].ToString();
                                                    String entity_data = entities[i]["entity"].ToString();
                                                    entity_data = Regex.Replace(entity_data, " ", "");
                                                    //temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entities[j]["entity"].ToString() + "',";
                                                    if (entity_type.Equals("customer_comment") || entity_type.Equals("customer_comment"))
                                                    {
                                                        //nothing--remove parameter data    
                                                    }
                                                    else
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[i]["type"].ToString() + "='" + entity_data + "',";
                                                    }
                                                    temp_paramEntities = temp_paramEntities.Substring(0, temp_paramEntities.Length - 1);

                                                }
                                            }
                                        }

                                        db_update_check = db.UpdateDeliveryData(etc_data, comment_data, temp_paramEntities);
                                        deliveryData = new List<DeliveryData>();
                                        deliveryData = db.SelectDeliveryData(temp_paramEntities);
                                        
                                        String invoice_num2 = null;
                                        String delivery_type = null;
                                        String customer_name = null;
                                        String etc = null;
                                        String customer_comment = null;
                                        String pay_type = null;
                                        String fees = null;
                                       
                                        invoice_num2 = deliveryData[0].invoice_num2 + "/";
                                        delivery_type = deliveryData[0].delivery_type + "/";
                                        customer_name = deliveryData[0].customer_name + "/";
                                        etc = deliveryData[0].etc + "/";
                                        customer_comment = deliveryData[0].customer_comment + "/";
                                        pay_type = deliveryData[0].pay_type + "/";
                                        fees = deliveryData[0].fees + "/";
                                        String input_text = "";
                                        input_text = "'"+etc + customer_comment + "' 내용으로 등록되었습니다.<hr>";

                                        String strComment = "";
                                        strComment = "송장번호 : " + deliveryData[0].invoice_num2 + "/";
                                        strComment += "이름 : " + deliveryData[0].customer_name + "/";
                                        strComment += "집배송구분 : " + deliveryData[0].delivery_type + "/";
                                        strComment += "착불여부 : " + deliveryData[0].pay_type + "/";
                                        strComment += "수수료 : " + deliveryData[0].fees + "/";

                                        dlg.cardText = dlg.cardText.Replace("@deliveryData@", input_text + strComment);

                                    }
                                    else {
                                        for (var j = 0; j < entities.Count(); j++)
                                        {
                                            for (int ii = 0; ii < column_name.Length; ii++)
                                            {
                                                if (entities[j]["type"].ToString().Equals(column_name[ii]))
                                                {
                                                    String entity_type = entities[j]["type"].ToString();
                                                    String entity_data = entities[j]["entity"].ToString();
                                                    entity_data = Regex.Replace(entity_data, " ", "");
                                                    //temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entities[j]["entity"].ToString() + "',";
                                                    if (entity_type.Equals("address_old") || entity_type.Equals("address_new"))//주소일때는 like 검색
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + " like '%" + entity_data + "%',";
                                                    }
                                                    else if ((entity_type.Equals("above")))//이상
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + ">='" + entity_data + "',";
                                                    }
                                                    else if ((entity_type.Equals("below")))//이하
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "<='" + entity_data + "',";
                                                    }
                                                    else//나머지..
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entity_data + "',";
                                                    }
                                                }
                                            }
                                        }
                                        temp_paramEntities = temp_paramEntities.Substring(0, temp_paramEntities.Length - 1);
                                        String param_entities = temp_paramEntities;

                                        deliveryData = db.SelectDeliveryData(param_entities);
                                        String deliveryDataText = "";
                                        int deliveryDataCount_ = 0;

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
                                        String deliveryDataCount = null;
                                        String separate_line = "<hr>";

                                        String account_text = "";
                                        String intent_text = "";
                                        String smsMsg = "";

                                    if (deliveryData != null)
                                        {
                                            deliveryDataCount_ = deliveryData.Count;
                                            deliveryDataCount = deliveryDataCount_.ToString();
                                            
                                            deliveryDataCount = deliveryData.Count.ToString();
                                            if (param_intent.Equals("문자안내전송"))
                                            {
                                                
                                                intent_text = "다음의 사항을 문자전송하였습니다.<hr>";
                                                for (var z = 0; z < entities.Count(); z++)
                                                {
                                                    String temp_ent = entities[z]["type"].ToString();
                                                    temp_ent = Regex.Replace(temp_ent, " ", "");
                                                    if (temp_ent.Equals("sms_msg"))
                                                    {
                                                        smsMsg = Regex.Replace(entities[z]["entity"].ToString(), " ", "");
                                                    }
                                                }
                                        }

                                            for (var z = 0; z < entities.Count(); z++)
                                            {
                                                String temp_ent = entities[z]["entity"].ToString();
                                                temp_ent = Regex.Replace(temp_ent, " ", "");
                                                if (temp_ent.Equals("계좌정보"))
                                                {
                                                //account_temp = 1;
                                                account_text = "계좌정보(우리은행:12345-45678-78 예금주:CJ대한통운)<hr>";
                                                break;
                                                }
                                            }
                                           
                                            if (deliveryData.Count == 0)
                                            {
                                            //dlg.cardText = dlg.cardText.Replace("@deliveryData@", "해당 조건에 맞는 정보가 존재하지 않습니다.");
                                            dlg.cardText = "해당 조건에 맞는 정보가 존재하지 않습니다.";
                                            }
                                            else if (deliveryData.Count == 1)
                                            {
                                                invoice_num1 = deliveryData[0].invoice_num1;
                                                invoice_num2 = deliveryData[0].invoice_num2;
                                                delivery_type = deliveryData[0].delivery_type;
                                                part = deliveryData[0].part;
                                                customer_name = deliveryData[0].customer_name;
                                                address_old = deliveryData[0].address_old;
                                                address_new = deliveryData[0].address_new;
                                                phone = deliveryData[0].phone;
                                                box_type = deliveryData[0].box_type;
                                                commission_place = deliveryData[0].commission_place;
                                                etc = deliveryData[0].etc;
                                                customer_comment = deliveryData[0].customer_comment;
                                                pay_type = deliveryData[0].pay_type;
                                                fees = deliveryData[0].fees;
                                                quantity = deliveryData[0].quantity;
                                                book_type = deliveryData[0].book_type;
                                                /*
                                                 * 시간 형식으로 표시
                                                 */
                                                if(deliveryData[0].delivery_time==null|| deliveryData[0].delivery_time.Equals("")){
                                                delivery_time = "";
                                                }
                                                else
                                                {
                                                    delivery_time = deliveryData[0].delivery_time.Substring(0, deliveryData[0].delivery_time.Length - 2);
                                                    delivery_time = delivery_time + ":00\n";
                                                }
                                                
                                            //delivery_time = deliveryData[0].delivery_time + "/";
                                                delivery_status = deliveryData[0].delivery_status;
                                                store_num = deliveryData[0].store_num;
                                                store_name = deliveryData[0].store_name;
                                                sm_num = deliveryData[0].sm_num;
                                                sm_name = deliveryData[0].sm_name;

                                                dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM1@", invoice_num1);
                                                dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM2@", invoice_num2);
                                                dlg.cardText = dlg.cardText.Replace("@DELIVERY_TYPE@", delivery_type);
                                                dlg.cardText = dlg.cardText.Replace("@PART@", part);
                                                dlg.cardText = dlg.cardText.Replace("@CUSTOMER_NAME@", customer_name);
                                                dlg.cardText = dlg.cardText.Replace("@ADDRESS_OLD@", address_old);
                                                dlg.cardText = dlg.cardText.Replace("@ADDRESS_NEW@", address_new);
                                                dlg.cardText = dlg.cardText.Replace("@PHONE@", phone);
                                                dlg.cardText = dlg.cardText.Replace("@BOX_TYPE@", box_type);
                                                dlg.cardText = dlg.cardText.Replace("@COMMISSION_PLACE@", commission_place);
                                                dlg.cardText = dlg.cardText.Replace("@ETC@", etc);
                                                dlg.cardText = dlg.cardText.Replace("@CUSTOMER_COMMENT@", customer_comment);
                                                dlg.cardText = dlg.cardText.Replace("@PAY_TYPE@", pay_type);
                                                dlg.cardText = dlg.cardText.Replace("@FEES@", fees);
                                                dlg.cardText = dlg.cardText.Replace("@QUANTITY@", quantity);
                                                dlg.cardText = dlg.cardText.Replace("@BOOK_TYPE@", book_type);
                                                dlg.cardText = dlg.cardText.Replace("@DELIVERY_TIME@", delivery_time);
                                                dlg.cardText = dlg.cardText.Replace("@DELIVERY_STATUS@", delivery_status);
                                                dlg.cardText = dlg.cardText.Replace("@STORE_NUM@", store_num);
                                                dlg.cardText = dlg.cardText.Replace("@STORE_NAME@", store_name);
                                                dlg.cardText = dlg.cardText.Replace("@SM_NUM@", sm_num);
                                                dlg.cardText = dlg.cardText.Replace("@SM_NAME@", sm_name);

                                                dlg.cardText = dlg.cardText.Replace("@DELIVERY_COUNT@", deliveryDataCount);
                                                dlg.cardText = dlg.cardText.Replace("@SMS_MSG@", smsMsg);
                                            


                                                String sub_info = "";
                                                String invoice_num2Test = "<hr>송장번호: "+invoice_num2;

                                                for (var a = 0; a < entities.Count(); a++)
                                                {
                                               
                                                if (entities[a]["type"].ToString().Equals("r_delivery_type"))
                                                {
                                                    sub_info += invoice_num2Test + " / 집배송구분 : " + delivery_type + "/";
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_invoice_num2"))
                                                {
                                                    sub_info += invoice_num2Test+ "/";
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_fees"))
                                                {
                                                    sub_info += invoice_num2Test + " / 수수료 : " + fees + "/";
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_part"))
                                                    {
                                                        sub_info += invoice_num2Test +" / 구역 : " + part + "/";
                                                    }

                                                    //if (entities[a]["type"].ToString().Equals("r_address_old")|| entities[a]["type"].ToString().Equals("r_address_new"))
                                                    if (entities[a]["type"].ToString().Equals("r_address_old"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 지번주소 : " + address_old + "/ 도로명주소 : "+ address_new ;
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_phone"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 전화번호 : " + phone + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_box_type"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 박스구분 : " + box_type + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_commission_place"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 위탁정보 : " + commission_place + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_etc"))
                                                    {
                                                        if (etc == null || etc.Equals(""))
                                                        {
                                                            sub_info += invoice_num2Test + " /비고내용 은 없습니다./";
                                                            dlg.cardText = invoice_num2Test + "상품의 비고내용은 없습니다.";
                                                        }
                                                        else
                                                        {
                                                            sub_info += invoice_num2Test + " / 비고 : " + etc + "/";
                                                        }
                                                    
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_customer_comment"))
                                                    {
                                                        if(customer_comment==null||customer_comment.Equals(""))
                                                        {
                                                        sub_info += invoice_num2Test + " / 고객특성 은 없습니다./";
                                                        dlg.cardText = invoice_num2Test + "상품의 고객특성은 없습니다.";
                                                    }
                                                        else
                                                        {
                                                        sub_info += invoice_num2Test + " / 고객특성 : " + customer_comment + "/";
                                                        }
                                                   
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_pay_type"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 운임구분 : " + pay_type + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_book_type"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 예약구분 : " + book_type + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_delivery_time"))
                                                    {
                                                        delivery_time = deliveryData[0].delivery_time.Substring(0, deliveryData[0].delivery_time.Length - 2);
                                                        delivery_time = delivery_time + ":00\n";
                                                        sub_info += invoice_num2Test + " / 배달예정시간 : " + delivery_time + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_delivery_status"))
                                                    {
                                                    sub_info += invoice_num2Test + " / 상태정보 : " + delivery_status + "/";
                                                    }

                                                    if (entities[a]["type"].ToString().Equals("r_quantity"))
                                                    {
                                                        sub_info += invoice_num2Test + " / 수량 : " + quantity + "/";
                                                    }

                                                if (entities[a]["type"].ToString().Equals("delivery_info"))
                                                {
                                                    sub_info = "<hr>송장번호 : " + invoice_num2 + "/";
                                                    sub_info += "이름 : " + customer_name + "/";
                                                    sub_info += "집배송구분 : " + delivery_type + "/";
                                                    sub_info += "수수료 : " + fees + "/";
                                                }
                                            }
                                            dlg.cardText = dlg.cardText.Replace("@SUBINFO@", sub_info);
                                        }
                                            else
                                            {
                                            String sub_info = "";
                                                /*
                                                 * 한개 이상의 데이터가 나오므로 여기는 뭉텡이로
                                                 * 보여주어야 할 데이터를 만들어야 겠네요.
                                                 */
                                                int count_temp = 0;
                                                String count_text = "";
                                                deliveryDataCount = deliveryData.Count.ToString();
                                                for (var z = 0; z < entities.Count(); z++)
                                                {
                                                    if (entities[z]["type"].ToString().Equals("delivery_count"))
                                                    {
                                                        count_temp = 1;
                                                        break;
                                                    }
                                                }
                                                
                                                if (count_temp > 0)
                                                {
                                                    count_text = "결과건수 : " + deliveryDataCount + "<hr>";
                                                }

                                                account_text = "";
                                                
                                            for (var z = 0; z < entities.Count(); z++)
                                            {
                                                String temp_ent = entities[z]["entity"].ToString();
                                                temp_ent = Regex.Replace(temp_ent, " ", "");
                                                if (temp_ent.Equals("계좌정보"))
                                                {
                                                    //account_temp = 1;
                                                    account_text = "<hr>계좌정보(우리은행:12345-45678-78 예금주:CJ대한통운)<hr>";
                                                    break;
                                                }
                                            }

                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_COUNT@", deliveryDataCount);
                                            dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM1@", deliveryData[0].invoice_num1);
                                            dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM2@", deliveryData[0].invoice_num2);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_TYPE@", deliveryData[0].delivery_type);
                                            dlg.cardText = dlg.cardText.Replace("@PART@", deliveryData[0].part);
                                            dlg.cardText = dlg.cardText.Replace("@CUSTOMER_NAME@", deliveryData[0].customer_name);
                                            dlg.cardText = dlg.cardText.Replace("@ADDRESS_OLD@", deliveryData[0].address_old);
                                            dlg.cardText = dlg.cardText.Replace("@ADDRESS_NEW@", deliveryData[0].address_new);
                                            dlg.cardText = dlg.cardText.Replace("@PHONE@", deliveryData[0].phone);
                                            dlg.cardText = dlg.cardText.Replace("@BOX_TYPE@", deliveryData[0].box_type);
                                            dlg.cardText = dlg.cardText.Replace("@COMMISSION_PLACE@", deliveryData[0].commission_place);
                                            dlg.cardText = dlg.cardText.Replace("@ETC@", deliveryData[0].etc);
                                            dlg.cardText = dlg.cardText.Replace("@CUSTOMER_COMMENT@", deliveryData[0].customer_comment);
                                            dlg.cardText = dlg.cardText.Replace("@PAY_TYPE@", deliveryData[0].pay_type);
                                            dlg.cardText = dlg.cardText.Replace("@FEES@", deliveryData[0].fees);
                                            dlg.cardText = dlg.cardText.Replace("@QUANTITY@", deliveryData[0].quantity);
                                            dlg.cardText = dlg.cardText.Replace("@BOOK_TYPE@", deliveryData[0].book_type);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_TIME@", deliveryData[0].delivery_time);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_STATUS@", deliveryData[0].delivery_status);
                                            dlg.cardText = dlg.cardText.Replace("@STORE_NUM@", deliveryData[0].store_num);
                                            dlg.cardText = dlg.cardText.Replace("@STORE_NAME@", deliveryData[0].store_name);
                                            dlg.cardText = dlg.cardText.Replace("@SM_NUM@", deliveryData[0].sm_num);
                                            dlg.cardText = dlg.cardText.Replace("@SM_NAME@", deliveryData[0].sm_name);

                                            dlg.cardText = dlg.cardText.Replace("@SMS_MSG@", smsMsg);

                                            for (int i = 0; i < deliveryData.Count; i++)
                                                {
                                                    sub_info = "<hr>송장번호 : " + deliveryData[i].invoice_num2 + "/";
                                                    sub_info += "이름 : " + deliveryData[i].customer_name + "/";
                                                    sub_info += "집배송구분 : " + deliveryData[i].delivery_type + "/";
                                                    sub_info += "수수료 : " + deliveryData[i].fees + "/";


                                                for (var a = 0; a < entities.Count(); a++)
                                                {
                                                    for (int aa = 0; aa < column_name.Length; aa++)
                                                    {
                                                        if (entities[a]["type"].ToString().Equals(column_name[aa]))
                                                        {
                                                            if (entities[a]["type"].ToString().Equals("r_part"))
                                                            {
                                                                sub_info += "구역 : " + deliveryData[i].part + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_quantity"))
                                                            {
                                                                sub_info += "수량 : " + deliveryData[i].quantity + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_address_old"))
                                                            {
                                                                sub_info += "지번주소 : " + deliveryData[i].address_old + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_address_new"))
                                                            {
                                                                sub_info += "도로명주소 : " + deliveryData[i].address_new + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_phone"))
                                                            {
                                                                sub_info += "전화번호 : " + deliveryData[i].phone + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_box_type"))
                                                            {
                                                                sub_info += "박스구분 : " + deliveryData[i].box_type + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_commission_place"))
                                                            {
                                                                sub_info += "위탁정보 : " + deliveryData[i].commission_place + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_etc"))
                                                            {
                                                                sub_info += "비고 : " + deliveryData[i].etc + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_customer_comment"))
                                                            {
                                                                sub_info += "고객특성 : " + deliveryData[i].customer_comment + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_pay_type"))
                                                            {
                                                                sub_info += "운임구분 : " + deliveryData[i].pay_type + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_book_type"))
                                                            {
                                                                sub_info += "예약구분 : " + deliveryData[i].book_type + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_delivery_time"))
                                                            {
                                                                delivery_time = deliveryData[0].delivery_time.Substring(0, deliveryData[0].delivery_time.Length - 2);
                                                                delivery_time = delivery_time + ":00\n";
                                                                sub_info += "배달예정시간 : " + deliveryData[i].delivery_time + "/";
                                                            }

                                                            if (entities[a]["type"].ToString().Equals("r_delivery_status"))
                                                            {
                                                                sub_info += "상태정보 : " + deliveryData[i].delivery_status + "/";
                                                            }

                                                        }
                                                    }
                                                }

                                                //sub_info += separate_line;
                                                    deliveryDataText = deliveryDataText + sub_info;
                                                }
                                                dlg.cardText = dlg.cardText.Replace("@SUBINFO@", account_text+deliveryDataText);
                                            }
                                        }
                                        else
                                        {
                                            deliveryDataCount_ = 0;
                                            dlg.cardText = "해당 조건에 맞는 정보가 존재하지 않습니다.";
                                        }

                                    }
                                    
                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);

                                //}

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
                        text = db.SelectSorryDialogText("8");
                    }
                    else
                    {
                        //text = db.SelectSorryDialogText("6");
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