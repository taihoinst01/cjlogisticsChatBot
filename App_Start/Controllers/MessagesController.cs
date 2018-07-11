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
        public static List<DeliveryTypeList> deliveryTypeList = new List<DeliveryTypeList>();
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

                        ///////////////////////////////////////////////////////////////////////
                        //물량정보조회3의 entitites를 가격으로 임의 지정
                        if (luisIntent.Equals("물량정보조회2") || luisIntent.Equals("물량정보조회3") || luisIntent.Equals("물량정보조회4") || luisIntent.Equals("물량정보조회5"))
                        {
                            cacheList.luisEntities = "가격";  //임시설정
                        }
                        if (luisIntent.Equals("문자안내재전송"))
                        {
                            cacheList.luisEntities = "reSendMsg";
                        }
                        if (luisIntent.Equals("등록신청"))
                        {
                            cacheList.luisEntities = "insertEtcComment";
                        }

                        //orgMent 변경(문자수신 시나리오)
                        if (luisIntent.Equals("문자수신시나리오") || orgMent.Equals("예") || orgMent.Equals("아니오")
                            || orgMent.Equals("예스") || orgMent.Equals("노") || orgMent.Equals("yes") || orgMent.Equals("no"))
                        {
                            luisIntent = "문자수신시나리오";
                            cacheList.luisEntities = "smsScenario";
                        }

                        if (luisIntent.Equals("물량그룹건수조회"))
                        {
                            cacheList.luisEntities = "groupperentity";
                        }

                        DButil.HistoryLog("luisEntities : " + luisEntities);
                        ///////////////////////////////////////////////////////////////////////



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
                                Debug.WriteLine("param_intent===" + param_intent);
                                String temp_paramEntities = null;
                                String[] column_name = new String[] { "invoice_num1", "invoice_num2", "delivery_type", "part", "customer_name", "address_old", "address_new", "phone", "box_type", "commission_place", "etc", "customer_comment", "pay_type", "fees", "quantity", "book_type", "delivery_time", "delivery_status", "store_num", "store_name", "sm_num", "sm_name", "address_detail" };
                                int above_data = 0;
                                int below_data = 0;
                                String show_fees = "";
                                String show_address = "";
                                if (param_intent.Equals("물량정보조회2") || param_intent.Equals("물량정보조회3") || param_intent.Equals("물량정보조회4") || param_intent.Equals("물량정보조회5"))
                                {
                                    Debug.WriteLine("param_intent :: " + param_intent);
                                    DButil.HistoryLog("param_intent : " + param_intent);

                                    JArray _columnTitle = new JArray();
                                    JArray _columValue = new JArray();
                                    JArray _resultAnswer = new JArray();

                                    for (int i = 0; i < entities.Count(); i++)
                                    {
                                        var resultAnswerChk = entities[i]["type"].ToString().Substring(0, 2);
                                        if (resultAnswerChk.Equals("r_"))
                                        {   //검색하고자 하는 entity배열 (select할 부분)
                                            var answerEntity = entities[i]["type"].ToString().Substring(2, entities[i]["type"].ToString().Length - 2);
                                            _resultAnswer.Add(answerEntity);
                                        }
                                        else
                                        {   //조건에 맞는 entity 배열 (where절에 들어갈것)

                                            //compositEntities가 있을때(송장번호)
                                            //if (entities[i]["type"].ToString().Equals("invoice_num1") || entities[i]["type"].ToString().Equals("invoice_num2"))
                                            //{
                                            //    compositEntities = dbutil.GetCompositEnities(orgMent);  //compositEntities 가져오는 부분

                                            //    for(int o=0; o< compositEntities.Count(); o++)
                                            //    {
                                            //        DButil.HistoryLog("compositEntities ::::: " + compositEntities[o]);
                                            //        if (compositEntities[o]["type"].ToString().Equals(entities[i]["type"].ToString()))
                                            //        {
                                            //            DButil.HistoryLog("compositEntities_value ::::: " + compositEntities[o]["value"].ToString());
                                            //            _columnTitle.Add(entities[i]["type"].ToString());
                                            //            _columValue.Add(compositEntities[o]["value"].ToString());
                                            //        }
                                            //    }
                                            //}
                                            for (int j = 0; j < column_name.Length; j++)
                                            {
                                                if (entities[i]["type"].ToString().Equals(column_name[j]))
                                                {
                                                    if (!entities[i]["type"].ToString().Equals("delivery_time") //배송시간 제외
                                                        || !entities[i]["type"].ToString().Equals("quantity")   //수량 제외
                                                        || !entities[i]["type"].ToString().Equals("fees"))      //운임 제외
                                                    {
                                                        _columnTitle.Add(entities[i]["type"].ToString());
                                                        _columValue.Add(Regex.Replace(entities[i]["entity"].ToString(), " ", ""));
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    deliveryData = db.SelectDeliveryData(_columnTitle, _columValue, _resultAnswer);

                                    if (deliveryData != null)
                                    {
                                        var oriTextData = dlg.cardText;
                                        int _tempFee = 0;
                                        var _sumFlag = 0;
                                        DButil.HistoryLog("deliveryData.Count ::::: " + deliveryData.Count);
                                        for (int i = 0; i < deliveryData.Count(); i++)
                                        {
                                            //데이터 셋팅
                                            var settingResult = "";
                                            if (i.Equals(0))
                                            {
                                                dlg.cardTitle = dlg.cardTitle + " (총 건수는 : " + deliveryData.Count() + "건 입니다.)";
                                            }

                                            for (int j = 0; j < _resultAnswer.Count(); j++)
                                            {
                                                for (int k = 0; k < column_name.Length; k++)
                                                {
                                                    if (column_name[k].Equals(_resultAnswer[j].ToString()))
                                                    {
                                                        if (_resultAnswer[j].ToString().Equals("invoice_num1"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].invoice_num1 + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("invoice_num2"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].invoice_num2 + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("delivery_type"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].delivery_type + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("part"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].part + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("customer_name"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].customer_name + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("address_old"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].address_old + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("address_new"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].address_new + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("phone"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].phone + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("box_type"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].box_type + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("commission_place") && !deliveryData[i].commission_place.Equals(""))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].commission_place + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("etc") && !deliveryData[i].etc.Equals(""))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].etc + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("customer_comment") && !deliveryData[i].customer_comment.Equals(""))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].customer_comment + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("pay_type"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].pay_type + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("fees"))
                                                        {
                                                            if (_resultAnswer.Count().Equals(1))
                                                            {
                                                                _sumFlag = 1;
                                                                _tempFee = _tempFee + Convert.ToInt32(deliveryData[i].fees);
                                                            }
                                                            else
                                                            {
                                                                settingResult = settingResult + deliveryData[i].fees + ", ";
                                                            }

                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("quantity"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].quantity + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("book_type"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].book_type + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("delivery_time") && !deliveryData[i].delivery_time.Equals(""))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].delivery_time + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("delivery_status"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].delivery_status + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("store_num"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].store_num + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("store_name"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].store_name + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("sm_num"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].sm_num + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("sm_name"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].sm_name + ", ";
                                                        }
                                                        else if (_resultAnswer[j].ToString().Equals("address_detail"))
                                                        {
                                                            settingResult = settingResult + deliveryData[i].sm_name + ", ";
                                                        }
                                                    }
                                                }
                                            }
                                            //Debug.WriteLine("settingResult :: " + settingResult);
                                            if (settingResult.Equals(""))
                                            {
                                                if (!_tempFee.Equals(0))
                                                {  //수수료 멘트
                                                    dlg.cardTitle = "정보 (총 건수는 : " + deliveryData.Count() + "건 입니다.)";
                                                    dlg.cardText = dlg.cardText.Replace("##DATA", _tempFee.ToString());
                                                }
                                                else
                                                {
                                                    dlg.cardTitle = "정보 (총 건수는 : 0 건 입니다.)";
                                                    dlg.cardText = dlg.cardText.Replace(dlg.cardText, "0 건의 정보는 조회되지 않습니다.");
                                                }
                                            }
                                            else
                                            {
                                                settingResult = settingResult.Substring(0, settingResult.Length - 2);
                                                dlg.cardText = dlg.cardText.Replace("##DATA", settingResult);
                                            }

                                            //카드 출력
                                            if (_sumFlag.Equals(1) && i.Equals(deliveryData.Count() - 1)) // 1건만 출력하기 위한 조건
                                            {
                                                tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                                commonReply.Attachments.Add(tempAttachment);
                                            }
                                            else if (_sumFlag.Equals(0))
                                            {
                                                tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                                commonReply.Attachments.Add(tempAttachment);
                                            }

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
                                else if (param_intent.Equals("문자안내재전송"))
                                {
                                    //activity.Conversation.Id  <-- history에 저장되는 userid
                                    //이전 메세지 찾기
                                    List<HistoryList> oldMent = db.OldMentChk(activity.Conversation.Id);
                                    var _temp1 = "";
                                    var _temp2 = "";
                                    var _temp3 = "";
                                    for (int j = 0; j < oldMent.Count(); j++)
                                    {
                                        //Debug.WriteLine("oldMent :: " + oldMent[j].customer_comment_kr);
                                        JArray oldMentEntity = new JArray();

                                        oldMentEntity = dbutil.GetEnities(oldMent[j].customer_comment_kr);  //entities 가져오는 부분

                                        for (int k = 0; k < oldMentEntity.Count(); k++)
                                        {
                                            // 3개만 적용됨(추후 더 추가될 가능성있음)
                                            if (oldMentEntity[k]["type"].ToString().Equals("invoice"))
                                            {
                                                _temp1 = Regex.Replace(oldMentEntity[k]["entity"].ToString(), " ", ""); //ex) 송장   
                                            }
                                            else if (oldMentEntity[k]["type"].ToString().Equals("invoice_num2"))
                                            {
                                                _temp2 = oldMentEntity[k]["entity"].ToString();                         //ex) 8115  
                                            }
                                            else if (oldMentEntity[k]["type"].ToString().Equals("sms_msg"))
                                            {
                                                _temp3 = Regex.Replace(oldMentEntity[k]["entity"].ToString(), " ", ""); //ex) 경비실위탁배송안내  
                                            }
                                        }
                                        break;
                                    }

                                    //변경되어 보낼 메세지 조립
                                    string oldMsg = "";
                                    string changeMsg = "";

                                    for (int i = 0; i < entities.Count(); i++)
                                    {
                                        if (entities[i]["type"].ToString().Equals("sms_msg"))
                                        {
                                            oldMsg = Regex.Replace(entities[i]["entity"].ToString(), " ", "");
                                        }
                                        else if (entities[i]["type"].ToString().Equals("sms_re_msg"))
                                        {
                                            changeMsg = Regex.Replace(entities[i]["entity"].ToString(), " ", "");
                                        }
                                    }

                                    if (_temp3.Equals(oldMsg))
                                    {
                                        dlg.cardTitle = "정보";
                                        dlg.cardText = dlg.cardText.Replace(dlg.cardText, _temp1 + _temp2 + " 고객님에게 " + changeMsg + "이라고 안내 문자 발송 했습니다.");
                                    }
                                    else
                                    {
                                        dlg.cardTitle = "정보";
                                        dlg.cardText = dlg.cardText.Replace(dlg.cardText, oldMsg + " 포함한 문자 보낸 내역이 없습니다.");
                                    }

                                    //카드 출력
                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);

                                }
                                else if (param_intent.Equals("물량그룹건수조회"))
                                {
                                    JArray compositEnties = new JArray();
                                    compositEnties = dbutil.GetCompositEnities(orgMent);  //entities 가져오는 부분
                                    Debug.WriteLine("*******************************full GetCompositEnities : " + compositEnties);
                                    String groupByParam = "";
                                    String whereParam = "";
                                    String checkParamData = "";
                                    String[] checkParamDataArray = null;
                                    //groupby 및 column에 들어올 데이터
                                    for (var j = 0; j < compositEnties.Count(); j++)
                                    {

                                        for (int ii = 0; ii < column_name.Length; ii++)
                                        {
                                            if (compositEnties[j]["type"].ToString().Equals(column_name[ii]))
                                            {
                                                String entity_data = compositEnties[j]["value"].ToString();
                                                entity_data = Regex.Replace(entity_data, " ", "");
                                                groupByParam = groupByParam + compositEnties[j]["type"].ToString() + "',";
                                                checkParamData = checkParamData + compositEnties[j]["type"].ToString() + "#";
                                            }
                                        }

                                        if (groupByParam == null || groupByParam.Equals(""))
                                        {

                                        }
                                        else
                                        {
                                            groupByParam = groupByParam.Substring(0, groupByParam.Length - 1);
                                        }
                                    }
                                    //where절에 들어올 데이터
                                    checkParamDataArray = checkParamData.Split(new string[] { "#" }, StringSplitOptions.None);
                                    for (var j = 0; j < entities.Count(); j++)
                                    {
                                        for (int ii = 0; ii < column_name.Length; ii++)
                                        {
                                            if (entities[j]["type"].ToString().Equals(column_name[ii]))
                                            {

                                                String entity_type = entities[j]["type"].ToString();
                                                String entity_data = entities[j]["entity"].ToString();
                                                entity_data = Regex.Replace(entity_data, " ", "");

                                                int temp = 0;
                                                for (int a = 0; a < checkParamDataArray.Length; a++)
                                                {
                                                    if (checkParamDataArray[a].Equals(entity_type))
                                                    {
                                                        temp = 1;
                                                    }
                                                }

                                                if (temp > 0)
                                                {
                                                    //groupby 에 있으니까 where 에는 없어야 한다.
                                                }
                                                else
                                                {
                                                    if (entity_type.Equals("address_old") || entity_type.Equals("address_new") || entity_type.Equals("address_detail"))//주소일때는 like 검색
                                                    {
                                                        show_address = entity_data;
                                                        whereParam = whereParam + "REPLACE(" + entities[j]["type"].ToString() + ",' ','') like '%" + entity_data + "%'#";
                                                    }
                                                    else if ((entity_type.Equals("fees")))//이상, 이하처리
                                                    {
                                                        show_fees = entity_data;
                                                        if (above_data > 0)
                                                        {
                                                            whereParam = whereParam + entities[j]["type"].ToString() + ">=" + entity_data + "#";
                                                        }
                                                        else if (below_data > 0)
                                                        {
                                                            whereParam = whereParam + entities[j]["type"].ToString() + "<=" + entity_data + "#";
                                                        }
                                                        else
                                                        {
                                                            whereParam = whereParam + entities[j]["type"].ToString() + "='" + entity_data + "'#";
                                                        }
                                                    }
                                                    else if ((entity_type.Equals("delivery_type")))//유의어 처리(배달,배송)
                                                    {
                                                        if (entity_data.Equals("배송"))
                                                        {
                                                            entity_data = "배달";
                                                        }
                                                        whereParam = whereParam + entities[j]["type"].ToString() + "='" + entity_data + "'#";
                                                    }
                                                    else//나머지..
                                                    {
                                                        whereParam = whereParam + entities[j]["type"].ToString() + "='" + entity_data + "'#";
                                                    }
                                                }



                                            }
                                        }
                                    }

                                    String whereParamData = "";
                                    if (whereParam == null || whereParam.Equals(""))
                                    {

                                    }
                                    else
                                    {
                                        String[] whereParamArray = null;

                                        whereParamArray = whereParam.Split(new string[] { "#" }, StringSplitOptions.None);
                                        for (int aa = 0; aa < whereParamArray.Length; aa++)
                                        {
                                            whereParamData = whereParamData + whereParamArray[aa];
                                        }
                                    }

                                    //db select
                                    deliveryTypeList = new List<DeliveryTypeList>();
                                    deliveryTypeList = db.SelectDeliveryGroupList(groupByParam, whereParamData);
                                    String per_string = "";

                                    if (deliveryTypeList == null)
                                    {
                                        per_string = "내용이 없습니다.";
                                    }
                                    else
                                    {
                                        per_string = "*`";
                                        for (int bb = 0; bb < deliveryTypeList.Count; bb++)
                                        {
                                            per_string += deliveryTypeList[bb].delivery_type + ": " + deliveryTypeList[bb].type_count + "건,";
                                        }
                                        per_string += "`";
                                    }
                                    dlg.cardText = dlg.cardText.Replace("@PERSTRING@", per_string);

                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);
                                }
                                else if (param_intent.Equals("문자수신시나리오"))
                                {
                                    //KSO
                                    string invoiceNum = "";
                                    if (orgMent.Equals("예") || orgMent.Equals("yes"))
                                    {
                                        dlg.cardText = dlg.cardText.Replace(dlg.cardText, "배달장소를 문앞으로 변경해주세요 라고 문자 왔습니다.");
                                    }
                                    else if (orgMent.Equals("아니오") || orgMent.Equals("no"))
                                    {
                                        dlg.cardText = dlg.cardText.Replace(dlg.cardText, "해당문자가 미확인 문자로 저장되었습니다.");
                                    }
                                    else if (orgMent.Equals("미확인 문자 읽어줘"))
                                    {
                                        //이전 메세지 찾기
                                        List<HistoryList> oldMent = db.OldMentChk(activity.Conversation.Id);
                                        for (int j = 0; j < oldMent.Count(); j++)
                                        {
                                            //Debug.WriteLine("oldMent :: " + oldMent[j].customer_comment_kr);
                                            JArray oldMentEntity = new JArray();

                                            oldMentEntity = dbutil.GetEnities(oldMent[j].customer_comment_kr);  //entities 가져오는 부분
                                            for (int k = 0; k < oldMentEntity.Count(); k++)
                                            {
                                                if (oldMentEntity[k]["type"].ToString().Equals("invoice_num2"))
                                                {
                                                    invoiceNum = oldMentEntity[k]["entity"].ToString();         //ex) 8115  
                                                }
                                            }

                                            if (!invoiceNum.Equals(""))
                                            {
                                                break;
                                            }
                                        }

                                        if (!invoiceNum.Equals(""))
                                        {
                                            deliveryData = db.InvoiceNumDeliveryData(invoiceNum);

                                            if (deliveryData != null)
                                            {
                                                dlg.cardText = dlg.cardText.Replace(dlg.cardText, "송장 " + deliveryData[0].invoice_num2 + ", " +
                                                    deliveryData[0].part + " 고객 " + deliveryData[0].customer_name + "에게 문자 왔습니다. 읽어드릴까요?");
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace(dlg.cardText, "송장 " + invoiceNum + " 은(는) 없는 송장 번호 입니다.");
                                            }
                                        }
                                        dlg.cardText = dlg.cardText.Replace(dlg.cardText, "미확인 문자 1건 있습니다. 송장" + deliveryData[0].invoice_num2 + ", " + deliveryData[0].customer_name + " 고객님께서 배달장소를 문앞으로 변경해주세요 라고 문자 보냈습니다.");
                                    }
                                    else
                                    {
                                        for (int i = 0; i < entities.Count(); i++)
                                        {
                                            if (entities[i]["type"].ToString().Equals("invoice_num1"))
                                            {
                                                invoiceNum = Regex.Replace(entities[i]["entity"].ToString(), " ", "");
                                            }
                                            else if (entities[i]["type"].ToString().Equals("invoice_num2"))
                                            {
                                                invoiceNum = Regex.Replace(entities[i]["entity"].ToString(), " ", "");
                                            }
                                        }

                                        if (!invoiceNum.Equals(""))
                                        {
                                            deliveryData = db.InvoiceNumDeliveryData(invoiceNum);

                                            if (deliveryData != null)
                                            {
                                                dlg.cardText = dlg.cardText.Replace(dlg.cardText, "송장 " + deliveryData[0].invoice_num2 + ", " +
                                                    deliveryData[0].part + " 고객 " + deliveryData[0].customer_name + "에게 문자 왔습니다. 읽어드릴까요?");
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace(dlg.cardText, "송장 " + invoiceNum + " 은(는) 없는 송장 번호 입니다.");
                                            }
                                        }
                                        else
                                        {
                                            dlg.cardText = dlg.cardText.Replace(dlg.cardText, "문자 수신 시나리오에 맞지 않는 문장입니다.");
                                        }
                                    }
                                    //카드 출력
                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);
                                }
                                else if (param_intent.Equals("등록신청"))
                                {

                                    String etc_data = "";
                                    String comment_data = "";
                                    /*
                                     * 문장에 보관함, 비밀번호란 말이 있으면 무조건 ETC 이다
                                     * */
                                    String comment_utt = orgMent;
                                    Boolean a1 = comment_utt.Contains("보관함");
                                    Boolean a2 = comment_utt.Contains("비밀번호");

                                    int db_update_check = 0;
                                    for (var i = 0; i < entities.Count(); i++)
                                    {

                                        if (entities[i]["type"].ToString().Equals("etc_msg"))
                                        {
                                            etc_data = entities[i]["entity"].ToString();
                                            etc_data = Regex.Replace(etc_data, " ", "");
                                        }

                                        if (entities[i]["type"].ToString().Equals("comment_msg"))
                                        {
                                            comment_data = entities[i]["entity"].ToString();
                                            comment_data = Regex.Replace(comment_data, " ", "");
                                        }


                                        for (int ii = 0; ii < column_name.Length; ii++)
                                        {
                                            if (entities[i]["type"].ToString().Equals(column_name[ii]))
                                            {
                                                String entity_type = entities[i]["type"].ToString();
                                                String entity_data = entities[i]["entity"].ToString();
                                                entity_data = Regex.Replace(entity_data, " ", "");

                                                if (etc_data.Equals("") || etc_data == null)
                                                {
                                                    //nothing--remove parameter data    
                                                }


                                                if (comment_data.Equals("") || comment_data == null)
                                                {
                                                    //nothing--remove parameter data    
                                                }

                                                if (entity_type.Equals("customer_comment") || entity_type.Equals("etc"))
                                                {
                                                    //nothing--remove parameter data    
                                                }
                                                else
                                                {
                                                    temp_paramEntities = temp_paramEntities + entities[i]["type"].ToString() + "='" + entity_data + "'#";
                                                }


                                                if (temp_paramEntities == null || temp_paramEntities == "")
                                                {

                                                }
                                                else
                                                {
                                                    temp_paramEntities = temp_paramEntities.Substring(0, temp_paramEntities.Length - 1);
                                                }


                                            }
                                        }
                                    }

                                    if (a1 == true || a2 == true)//무조건 etc data
                                    {
                                        if (etc_data.Equals("") || etc_data == null)
                                        {
                                            etc_data = comment_data;
                                            comment_data = "";
                                        }
                                    }

                                    db_update_check = db.UpdateDeliveryData(etc_data, comment_data, temp_paramEntities);
                                    deliveryData = new List<DeliveryData>();
                                    deliveryData = db.SelectDeliveryData(temp_paramEntities);

                                    dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM2@", deliveryData[0].invoice_num2);
                                    dlg.cardText = dlg.cardText.Replace("@CUSTOMER_NAME@", deliveryData[0].customer_name);
                                    dlg.cardText = dlg.cardText.Replace("@ETC_DATA@", etc_data);
                                    dlg.cardText = dlg.cardText.Replace("@COMMENT_DATA@", comment_data);

                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);

                                }
                                else
                                {
                                    for (var j = 0; j < entities.Count(); j++)
                                    {


                                        if (entities[j]["type"].ToString().Equals("above"))
                                        {
                                            above_data = 1;
                                        }

                                        if (entities[j]["type"].ToString().Equals("below"))
                                        {
                                            below_data = 1;
                                        }

                                        for (int ii = 0; ii < column_name.Length; ii++)
                                        {
                                            if (entities[j]["type"].ToString().Equals(column_name[ii]))
                                            {
                                                String entity_type = entities[j]["type"].ToString();
                                                String entity_data = entities[j]["entity"].ToString();
                                                entity_data = Regex.Replace(entity_data, " ", "");
                                                //temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entities[j]["entity"].ToString() + "',";
                                                if (entity_type.Equals("address_old") || entity_type.Equals("address_new") || entity_type.Equals("address_detail"))//주소일때는 like 검색
                                                {
                                                    show_address = entity_data;
                                                    temp_paramEntities = temp_paramEntities + "REPLACE(" + entities[j]["type"].ToString() + ",' ','') like '%" + entity_data + "%'#";
                                                }
                                                else if ((entity_type.Equals("fees")))//이상, 이하처리
                                                {
                                                    show_fees = entity_data;
                                                    if (above_data > 0)
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + ">=" + entity_data + "#";
                                                    }
                                                    else if (below_data > 0)
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "<=" + entity_data + "#";
                                                    }
                                                    else
                                                    {
                                                        temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entity_data + "'#";
                                                    }
                                                }
                                                else if ((entity_type.Equals("delivery_type")))//유의어 처리(배달,배송)
                                                {
                                                    if (entity_data.Equals("배송"))
                                                    {
                                                        entity_data = "배달";
                                                    }
                                                    temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entity_data + "'#";
                                                }
                                                else//나머지..
                                                {
                                                    temp_paramEntities = temp_paramEntities + entities[j]["type"].ToString() + "='" + entity_data + "'#";
                                                }
                                            }
                                        }
                                    }

                                    if (temp_paramEntities == null || temp_paramEntities.Equals(""))
                                    {

                                    }
                                    else
                                    {
                                        temp_paramEntities = temp_paramEntities.Substring(0, temp_paramEntities.Length - 1);
                                    }

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
                                    String address_detail = null;
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
                                    String deliveryDataCount = "0";

                                    String smsMsg = "";

                                    if (deliveryData != null)
                                    {
                                        deliveryDataCount_ = deliveryData.Count;
                                        deliveryDataCount = deliveryDataCount_.ToString();

                                        //deliveryDataCount = deliveryData.Count.ToString();
                                        if (param_intent.Equals("문자안내전송"))
                                        {

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
                                            address_detail = deliveryData[0].address_detail;
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
                                            if (deliveryData[0].delivery_time == null || deliveryData[0].delivery_time.Equals(""))
                                            {
                                                delivery_time = "";
                                            }
                                            else
                                            {
                                                delivery_time = deliveryData[0].delivery_time.Substring(0, deliveryData[0].delivery_time.Length - 2);
                                                delivery_time = delivery_time + ":00";
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
                                            dlg.cardText = dlg.cardText.Replace("@ADDRESS_DETAIL@", address_detail);
                                            dlg.cardText = dlg.cardText.Replace("@PHONE@", phone);
                                            dlg.cardText = dlg.cardText.Replace("@BOX_TYPE@", box_type);
                                            dlg.cardText = dlg.cardText.Replace("@COMMISSION_PLACE@", commission_place);
                                            dlg.cardText = dlg.cardText.Replace("@ETC@", etc);
                                            dlg.cardText = dlg.cardText.Replace("@CUSTOMER_COMMENT@", customer_comment);
                                            dlg.cardText = dlg.cardText.Replace("@PAY_TYPE@", pay_type);
                                            dlg.cardText = dlg.cardText.Replace("@FEES@", fees);
                                            dlg.cardText = dlg.cardText.Replace("@SHOWFEES@", show_fees);
                                            dlg.cardText = dlg.cardText.Replace("@SHOWADDRESS@", show_address);
                                            dlg.cardText = dlg.cardText.Replace("@QUANTITY@", quantity);
                                            dlg.cardText = dlg.cardText.Replace("@BOOK_TYPE@", book_type);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_TIME@", delivery_time);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_STATUS@", delivery_status);
                                            dlg.cardText = dlg.cardText.Replace("@STORE_NUM@", store_num);
                                            dlg.cardText = dlg.cardText.Replace("@STORE_NAME@", store_name);
                                            dlg.cardText = dlg.cardText.Replace("@SM_NUM@", sm_num);
                                            dlg.cardText = dlg.cardText.Replace("@SM_NAME@", sm_name);

                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_COUNT@", deliveryDataCount);
                                            dlg.cardText = dlg.cardText.Replace("@SMS_MSG@", "\"" + smsMsg + "\"");



                                            String sub_info = "";
                                            //String invoice_num2Test = "---송장번호: " + invoice_num2;
                                            String invoice_num2Test = "";
                                            int show_subinfo = 0;

                                            for (var a = 0; a < entities.Count(); a++)
                                            {
                                                if (entities[a]["type"].ToString().Equals("r_delivery_type"))
                                                {
                                                    sub_info += invoice_num2Test + " **집배송구분 : " + delivery_type + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_invoice_num2"))
                                                {
                                                    sub_info += invoice_num2Test;
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_fees"))
                                                {
                                                    sub_info += invoice_num2Test + " **운임 : " + fees + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_part"))
                                                {
                                                    sub_info += invoice_num2Test + " **구역 : " + part + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_address_old") || entities[a]["type"].ToString().Equals("r_address_new"))
                                                //if (entities[a]["type"].ToString().Equals("r_address_old"))
                                                {
                                                    sub_info += invoice_num2Test + " **지번주소 : " + address_old + " ,도로명주소 : " + address_new + " ,상세주소 : " + address_detail + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_phone"))
                                                {
                                                    sub_info += invoice_num2Test + " **전화번호 : " + phone + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_box_type"))
                                                {
                                                    sub_info += invoice_num2Test + " **박스구분 : " + box_type + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_commission_place"))
                                                {
                                                    sub_info += invoice_num2Test + " **위탁정보 : " + commission_place + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_etc"))
                                                {
                                                    if (etc == null || etc.Equals(""))
                                                    {
                                                        sub_info += invoice_num2Test + "비고내용 은 없습니다.";
                                                        dlg.cardText = invoice_num2Test + "상품의 비고내용은 없습니다.";
                                                    }
                                                    else
                                                    {
                                                        sub_info += invoice_num2Test + " **비고 : " + etc + "**";
                                                    }
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_customer_comment"))
                                                {
                                                    if (customer_comment == null || customer_comment.Equals(""))
                                                    {
                                                        sub_info += invoice_num2Test + "고객특성 은 없습니다.";
                                                        dlg.cardText = invoice_num2Test + "상품의 고객특성은 없습니다.";
                                                    }
                                                    else
                                                    {
                                                        sub_info += invoice_num2Test + " **고객특성 : " + customer_comment + "**";
                                                    }
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_pay_type"))
                                                {
                                                    sub_info += invoice_num2Test + " **운임구분 : " + pay_type + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_book_type"))
                                                {
                                                    sub_info += invoice_num2Test + " **예약구분 : " + book_type + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_delivery_time"))
                                                {
                                                    delivery_time = deliveryData[0].delivery_time.Substring(0, deliveryData[0].delivery_time.Length - 2);
                                                    delivery_time = delivery_time + ":00";
                                                    sub_info += invoice_num2Test + " **배달예정시간 : " + delivery_time + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_delivery_status"))
                                                {
                                                    sub_info += invoice_num2Test + " **상태정보 : " + delivery_status + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("r_quantity"))
                                                {
                                                    sub_info += invoice_num2Test + " **수량 : " + quantity + "**";
                                                    show_subinfo = 1;
                                                }

                                                if (entities[a]["type"].ToString().Equals("delivery_info"))
                                                {
                                                    sub_info = "*` 송장번호 : " + invoice_num2; ;
                                                    sub_info += " ,이름 : " + customer_name + "`";
                                                    show_subinfo = 1;
                                                }

                                                if (show_subinfo == 0)
                                                {
                                                    sub_info = "*` 송장번호 : " + invoice_num2;
                                                    sub_info += " ,이름 : " + customer_name + "`";
                                                }

                                            }
                                            dlg.cardText = dlg.cardText.Replace("@SUBINFO@", sub_info);

                                            if (param_intent.Equals("물량정보조회"))
                                            {
                                                String type_string = "";
                                                deliveryTypeList = db.SelectDeliveryTypeList(temp_paramEntities);

                                                if (deliveryTypeList == null)
                                                {
                                                    type_string = "* `배달 :0건, 집화 :0건`";
                                                }
                                                else
                                                {

                                                    if (deliveryTypeList[0].delivery_type.Equals("집화"))
                                                    {
                                                        type_string = "* `배달 :0건, 집화 :" + deliveryTypeList[0].type_count + "건`";
                                                    }
                                                    else
                                                    {
                                                        type_string = "* `배달 :" + deliveryTypeList[0].type_count + "건, 집화 :0건`";
                                                    }
                                                }
                                                dlg.cardText = dlg.cardText.Replace("@TYPESTRING@", type_string);
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("@TYPESTRING@", "");
                                            }
                                        }
                                        else
                                        {
                                            String sub_info = "";
                                            deliveryTypeList = new List<DeliveryTypeList>();
                                            /*
                                             * 한개 이상의 데이터
                                             */
                                            int count_temp = 0;
                                            String count_text = "";

                                            deliveryDataCount_ = 0;
                                            deliveryDataCount_ = deliveryData.Count;
                                            deliveryDataCount = deliveryDataCount_.ToString();

                                            //deliveryDataCount = deliveryData.Count.ToString();
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
                                                count_text = "결과건수 : " + deliveryDataCount + " ";
                                            }

                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_COUNT@", deliveryDataCount);
                                            dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM1@", deliveryData[0].invoice_num1);
                                            dlg.cardText = dlg.cardText.Replace("@INVOICE_NUM2@", deliveryData[0].invoice_num2);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_TYPE@", deliveryData[0].delivery_type);
                                            dlg.cardText = dlg.cardText.Replace("@PART@", deliveryData[0].part);
                                            dlg.cardText = dlg.cardText.Replace("@CUSTOMER_NAME@", deliveryData[0].customer_name);
                                            dlg.cardText = dlg.cardText.Replace("@ADDRESS_OLD@", deliveryData[0].address_old);
                                            dlg.cardText = dlg.cardText.Replace("@ADDRESS_NEW@", deliveryData[0].address_new);
                                            dlg.cardText = dlg.cardText.Replace("@ADDRESS_DETAIL@", deliveryData[0].address_detail);
                                            dlg.cardText = dlg.cardText.Replace("@PHONE@", deliveryData[0].phone);
                                            dlg.cardText = dlg.cardText.Replace("@BOX_TYPE@", deliveryData[0].box_type);
                                            dlg.cardText = dlg.cardText.Replace("@COMMISSION_PLACE@", deliveryData[0].commission_place);
                                            dlg.cardText = dlg.cardText.Replace("@ETC@", deliveryData[0].etc);
                                            dlg.cardText = dlg.cardText.Replace("@CUSTOMER_COMMENT@", deliveryData[0].customer_comment);
                                            dlg.cardText = dlg.cardText.Replace("@PAY_TYPE@", deliveryData[0].pay_type);
                                            dlg.cardText = dlg.cardText.Replace("@FEES@", deliveryData[0].fees);
                                            dlg.cardText = dlg.cardText.Replace("@SHOWFEES@", show_fees);
                                            dlg.cardText = dlg.cardText.Replace("@SHOWADDRESS@", show_address);
                                            dlg.cardText = dlg.cardText.Replace("@QUANTITY@", deliveryData[0].quantity);
                                            dlg.cardText = dlg.cardText.Replace("@BOOK_TYPE@", deliveryData[0].book_type);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_TIME@", deliveryData[0].delivery_time);
                                            dlg.cardText = dlg.cardText.Replace("@DELIVERY_STATUS@", deliveryData[0].delivery_status);
                                            dlg.cardText = dlg.cardText.Replace("@STORE_NUM@", deliveryData[0].store_num);
                                            dlg.cardText = dlg.cardText.Replace("@STORE_NAME@", deliveryData[0].store_name);
                                            dlg.cardText = dlg.cardText.Replace("@SM_NUM@", deliveryData[0].sm_num);
                                            dlg.cardText = dlg.cardText.Replace("@SM_NAME@", deliveryData[0].sm_name);

                                            dlg.cardText = dlg.cardText.Replace("@SMS_MSG@", "\"" + smsMsg + "\"");

                                            for (int i = 0; i < deliveryData.Count; i++)
                                            {
                                                for (var a = 0; a < entities.Count(); a++)
                                                {
                                                    if (entities[a]["type"].ToString().Equals("delivery_info"))
                                                    {
                                                        sub_info += "* `송장번호 : " + deliveryData[i].invoice_num2;
                                                        sub_info += " ,이름 : " + deliveryData[i].customer_name + "`";
                                                    }
                                                    else
                                                    {
                                                        sub_info += "";
                                                    }

                                                }
                                            }




                                            if (param_intent.Equals("물량정보조회"))
                                            {
                                                String type_string = "";

                                                deliveryTypeList = db.SelectDeliveryTypeList(temp_paramEntities);

                                                if (deliveryTypeList == null)
                                                {
                                                    type_string = "* `배달 :0건, 집화 :0건`";
                                                }
                                                else
                                                {
                                                    if (deliveryTypeList.Count == 1)
                                                    {
                                                        if (deliveryTypeList[0].delivery_type.Equals("집화"))
                                                        {
                                                            type_string = "* `배달 :0건, 집화 :" + deliveryTypeList[0].type_count + "건`";
                                                        }
                                                        else
                                                        {
                                                            type_string = "* `배달 :" + deliveryTypeList[0].type_count + "건, 집화 :0건`";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        type_string = "* `배달 :" + deliveryTypeList[0].type_count + "건, 집화 :" + deliveryTypeList[1].type_count + "건`";
                                                    }

                                                }
                                                dlg.cardText = dlg.cardText.Replace("@TYPESTRING@", type_string);

                                                if (deliveryDataCount_ > 10)
                                                {
                                                    dlg.cardText = dlg.cardText.Replace("@SUBINFO@", "");
                                                }
                                                else
                                                {
                                                    if (sub_info.Equals("") || sub_info == null)
                                                    {
                                                        dlg.cardText = dlg.cardText.Replace("@SUBINFO@", "");
                                                    }
                                                    else
                                                    {
                                                        dlg.cardText = dlg.cardText.Replace("@SUBINFO@", sub_info);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                dlg.cardText = dlg.cardText.Replace("@TYPESTRING@", "");
                                            }



                                        }
                                    }
                                    else
                                    {
                                        deliveryDataCount_ = 0;
                                        dlg.cardTitle = dlg.cardTitle.Replace(dlg.cardTitle, "정보");
                                        dlg.cardText = "해당 조건에 맞는 정보가 존재하지 않습니다.";
                                    }

                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);

                                }



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
                            Debug.WriteLine("no dialogue-------------");
                            string newUserID = activity.Conversation.Id;
                            string beforeUserID = "";
                            string beforeMessgaeText = "";
                            //string messgaeText = "";

                            Activity intentNoneReply = activity.CreateReply();

                            if (beforeUserID != newUserID)
                            {
                                beforeUserID = newUserID;
                                MessagesController.sorryMessageCnt = 0;
                            }

                            var message = MessagesController.queryStr;
                            beforeMessgaeText = message.ToString();

                            Debug.WriteLine("SERARCH MESSAGE : " + message);

                            Activity sorryReply = activity.CreateReply();
                            sorryReply.Recipient = activity.From;
                            sorryReply.Type = "message";
                            sorryReply.Attachments = new List<Attachment>();
                            sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                            List<TextList> text = new List<TextList>();
                            text = db.SelectSorryDialogText("5");
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
                            replyresult = "D";

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
                    text = db.SelectSorryDialogText("5");

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