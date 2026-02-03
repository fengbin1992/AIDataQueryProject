namespace AIDataQuery.API.Models.Enums;

/// <summary>
/// 数据脱敏类型
/// </summary>
public enum MaskType
{
    /// <summary>
    /// 手机号 - 保留前3后4
    /// </summary>
    Phone = 1,

    /// <summary>
    /// 身份证 - 保留前3后4
    /// </summary>
    IdCard = 2,

    /// <summary>
    /// 邮箱 - 保留首字母和域名
    /// </summary>
    Email = 3,

    /// <summary>
    /// 银行卡 - 保留前4后4
    /// </summary>
    BankCard = 4,

    /// <summary>
    /// 姓名 - 保留姓氏
    /// </summary>
    Name = 5,

    /// <summary>
    /// 地址 - 保留省市区
    /// </summary>
    Address = 6,

    /// <summary>
    /// 金额 - 完全隐藏
    /// </summary>
    Amount = 7,

    /// <summary>
    /// 完全隐藏
    /// </summary>
    Full = 8,

    /// <summary>
    /// 自定义正则
    /// </summary>
    Custom = 99
}
