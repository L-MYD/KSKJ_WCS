namespace WCS_IServices
{
    /// <summary>
    /// PLC（可编程控制器）通讯服务接口。
    /// 由 WCS_Services 层的 PLCService 实现；注意静态方法/字段不属于接口，这里仅声明实例成员。
    /// </summary>
    public interface IPLCService
    {
        /// <summary>清除字符串中的特殊/非法字符</summary>
        string DeleteChar(string str);
    }
}
