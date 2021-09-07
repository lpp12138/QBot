//TODO:是否需要把各类消息类型拆分开来,通过关键字判断后再进行json解析?
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace jsonDataStructs
{
    class jsonStruct_receivedData
    {
        public Int64 time{get;set;}
        public Int64 self_id{ get; set; }
        /// <summary>
        /// message notice request meta_event
        /// 暂时只响应message
        /// </summary>
        public string post_type{get;set;}
        /// <summary>
        /// private:私聊
        /// group:群组
        /// </summary>
        public string message_type{get;set;}
        /// <summary>
        /// 私聊消息:
        /// friend->好友
        /// group->群临时会话
        /// other
        /// 群消息:
        /// normal->正常消息
        /// anonymous->匿名消息
        /// notice->系统提示
        /// </summary>
        public string sub_type{get;set;}
        public Int64 message_id{get;set;}
        public Int64 user_id{get;set;}
        public string message{get;set;}//注意这里消息类型在标准中允许使用字符串和数组两种数据类型,可能需要在服务端配置
        public string raw_message{get;set;}
        public Int64 font{get;set;}
        //public Sender sender{get;set;}
        //----以下仅限群消息
        public Int64 group_id{get;set;}
        /// <summary>
        /// 匿名消息,若不匿名则为null
        /// </summary>
        //public object anonymous{get;set;}
    }
    class Sender
    {
        Int64 user_id{get;set;}
        string nickname{get;set;}
        string sex{get;set;}
        string age{get;set;}
        //以下仅限群消息
        string card{get;set;}
        string area{get;set;}
        string level{get;set;}
        /// <summary>
        /// owner->群主
        /// admin->管理员
        /// member->群员
        /// </summary>
        string role{get;set;}
        string title{get;set;}
    }
    class jsonStruct_send_private_message
    {
        public Int64 user_id{get;set;}
        public string message{get;set;}
        public bool auto_escape { get; set; }//是否解析CQ码
    }
    class jsonStruct_send_group_message
    {
        public Int64 group_id{get;set;}
        public string message{get;set;}
        public bool auto_escape = false;
    }
    class jsonStruct_send_message
    {
        /// <summary>
        /// group->群组
        /// private->私聊
        /// </summary>
        public string message_type{get;set;}
        public Int64 user_id{get;set;}
        public Int64 group_id{get;set;}
        public string message{get;set;}
        public bool auto_escape = true;
    }
    class jsonStruct_api
    {
        public string action{get;set;}
        [JsonPropertyName("params")]
        public object param {get;set;}
        public Int64 echo{get;set;}
    }

}
