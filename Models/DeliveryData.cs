using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace cjlogisticsChatBot.Models
{
    [Serializable]
    public class DeliveryData
    {
        public string invoice_num1;
        public string invoice_num2;
        public string delivery_type;
        public string part;
        public string customer_name;
        public string address_old;
        public string address_new;
        public string address_detail;
        public string phone;

        public string box_type;
        public string commission_place;
        public string etc;
        public string customer_comment;
        public string pay_type;
        public string fees;
        public string quantity;
        public string book_type;
        public string delivery_time;
        public string delivery_status;
        public string store_num;
        public string store_name;
        public string sm_num;
        public string sm_name;
    }
}
