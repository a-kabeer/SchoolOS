using System.Globalization;

namespace SchoolOS.BuildingBlocks.Localization;

/// <summary>
/// Server-side multilingual validation and authorization error catalog covering EN, UR, and AR
/// with guaranteed key parity across all supported languages.
/// </summary>
public static class LocalizedValidationMessages
{
    public static class Keys
    {
        public const string Required = nameof(Required);
        public const string EmailInvalid = nameof(EmailInvalid);
        public const string PhoneInvalid = nameof(PhoneInvalid);
        public const string HexColorInvalid = nameof(HexColorInvalid);
        public const string PasswordLength = nameof(PasswordLength);
        public const string PasswordCommon = nameof(PasswordCommon);
        public const string PasswordContainsAccount = nameof(PasswordContainsAccount);
        public const string DomainFormat = nameof(DomainFormat);
        public const string DomainTaken = nameof(DomainTaken);
        public const string ImageUnsupportedType = nameof(ImageUnsupportedType);
        public const string ImageTooLarge = nameof(ImageTooLarge);
        public const string SvgRejected = nameof(SvgRejected);
        public const string StudentAgeOutOfRange = nameof(StudentAgeOutOfRange);
        public const string EnquiryAlreadyAdmitted = nameof(EnquiryAlreadyAdmitted);
        public const string TenantRequired = nameof(TenantRequired);
        public const string ModuleNotEnabled = nameof(ModuleNotEnabled);
        public const string PasswordChangeRequired = nameof(PasswordChangeRequired);
        public const string PermissionDenied = nameof(PermissionDenied);
    }

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Keys.Required] = "A value is required.",
        [Keys.EmailInvalid] = "Enter a valid email address.",
        [Keys.PhoneInvalid] = "Phone number must contain between 7 and 15 digits.",
        [Keys.HexColorInvalid] = "Color must use #RRGGBB hexadecimal format.",
        [Keys.PasswordLength] = "Password must be between {0} and {1} characters.",
        [Keys.PasswordCommon] = "That password appears on common breached-password lists.",
        [Keys.PasswordContainsAccount] = "Password cannot contain part of your name or email address.",
        [Keys.DomainFormat] = "Enter a valid host name (letters, digits, hyphens, and dots only).",
        [Keys.DomainTaken] = "That domain is already assigned to another school.",
        [Keys.ImageUnsupportedType] = "Unsupported image signature. Only PNG, JPEG, WebP, GIF, or ICO files are allowed.",
        [Keys.ImageTooLarge] = "Image size exceeds the {0} KB limit.",
        [Keys.SvgRejected] = "SVG and active markup payloads are rejected for security reasons.",
        [Keys.StudentAgeOutOfRange] = "Student age must be between 3 and 30 years.",
        [Keys.EnquiryAlreadyAdmitted] = "This enquiry has already been converted to an admitted student.",
        [Keys.TenantRequired] = "An authenticated tenant context is required for this request.",
        [Keys.ModuleNotEnabled] = "Your active subscription or license lease does not enable module '{0}'.",
        [Keys.PasswordChangeRequired] = "You must change your one-time password before accessing operational endpoints.",
        [Keys.PermissionDenied] = "Your role does not grant '{0}' access for this module."
    };

    private static readonly IReadOnlyDictionary<string, string> Urdu = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Keys.Required] = "یہ خانہ پر کرنا ضروری ہے۔",
        [Keys.EmailInvalid] = "براہِ کرم درست ای میل پتہ درج کریں۔",
        [Keys.PhoneInvalid] = "فون نمبر 7 سے 15 ہندسوں پر مشتمل ہونا چاہیے۔",
        [Keys.HexColorInvalid] = "رنگ کا کوڈ #RRGGBB ہیکسا ڈیسیمل شکل میں ہونا چاہیے۔",
        [Keys.PasswordLength] = "پاس ورڈ {0} سے {1} حروف کے درمیان ہونا چاہیے۔",
        [Keys.PasswordCommon] = "یہ پاس ورڈ عام لیک شدہ پاس ورڈز کی فہرست میں شامل ہے۔",
        [Keys.PasswordContainsAccount] = "پاس ورڈ میں آپ کے نام یا ای میل کا حصہ شامل نہیں ہو سکتا۔",
        [Keys.DomainFormat] = "درست ڈومین نام درج کریں (صرف حروف، ہندسے، ڈیش اور نقطے)۔",
        [Keys.DomainTaken] = "یہ ڈومین پہلے ہی کسی دوسرے اسکول کو تفویض ہے۔",
        [Keys.ImageUnsupportedType] = "تصویر کی قسم درست نہیں۔ صرف PNG، JPEG، WebP، GIF یا ICO فائلیں قابلِ قبول ہیں۔",
        [Keys.ImageTooLarge] = "تصویر کا حجم {0} KB کی حد سے زیادہ ہے۔",
        [Keys.SvgRejected] = "سیکیورٹی وجوہات کی بنا پر SVG اور اسکرپٹ فائلیں مسترد کر دی گئی ہیں۔",
        [Keys.StudentAgeOutOfRange] = "طالب علم کی عمر 3 سے 30 سال کے درمیان ہونی چاہیے۔",
        [Keys.EnquiryAlreadyAdmitted] = "یہ انکوائری پہلے ہی داخل شدہ طالب علم میں تبدیل ہو چکی ہے۔",
        [Keys.TenantRequired] = "اس درخواست کے لیے تصدیق شدہ اسکول (ٹیننٹ) سیاق و سباق ضروری ہے۔",
        [Keys.ModuleNotEnabled] = "آپ کے فعال سبسکرپشن یا لائسنس لیز میں ماڈیول '{0}' شامل نہیں ہے۔",
        [Keys.PasswordChangeRequired] = "آگے بڑھنے سے پہلے اپنا عارضی پاس ورڈ تبدیل کرنا لازمی ہے۔",
        [Keys.PermissionDenied] = "آپ کے کردار کو اس ماڈیول کے لیے '{0}' کی اجازت نہیں ہے۔"
    };

    private static readonly IReadOnlyDictionary<string, string> Arabic = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Keys.Required] = "هذا الحقل مطلوب.",
        [Keys.EmailInvalid] = "أدخل عنوان بريد إلكتروني صحيحاً.",
        [Keys.PhoneInvalid] = "يجب أن يتكون رقم الهاتف من 7 إلى 15 رقماً.",
        [Keys.HexColorInvalid] = "يجب أن يكون اللون بصيغة ‎#RRGGBB الست عشرية.",
        [Keys.PasswordLength] = "يجب أن يتراوح طول كلمة المرور بين {0} و {1} حرفاً.",
        [Keys.PasswordCommon] = "كلمة المرور هذه موجودة في قوائم كلمات المرور المسربة.",
        [Keys.PasswordContainsAccount] = "لا يمكن أن تحتوي كلمة المرور على جزء من اسمك أو بريدك الإلكتروني.",
        [Keys.DomainFormat] = "أدخل اسم نطاق صحيحاً (أحرف وأرقام وشرطات ونقاط فقط).",
        [Keys.DomainTaken] = "هذا النطاق مخصص بالفعل لمدرسة أخرى.",
        [Keys.ImageUnsupportedType] = "صيغة الصورة غير مدعومة. يُسمح فقط بملفات PNG أو JPEG أو WebP أو GIF أو ICO.",
        [Keys.ImageTooLarge] = "حجم الصورة يتجاوز الحد الأقصى البالغ {0} كيلوبايت.",
        [Keys.SvgRejected] = "يتم رفض ملفات SVG والمحتوى النشط لأسباب أمنية.",
        [Keys.StudentAgeOutOfRange] = "يجب أن يكون عمر الطالب بين 3 و 30 عاماً.",
        [Keys.EnquiryAlreadyAdmitted] = "تم تحويل هذا الاستفسار بالفعل إلى طالب مقبول.",
        [Keys.TenantRequired] = "مطلوب سياق مدرسة موثق لهذا الطلب.",
        [Keys.ModuleNotEnabled] = "لا يشمل اشتراكك أو ترخيصك الحالي الوحدة '{0}'.",
        [Keys.PasswordChangeRequired] = "يجب تغيير كلمة المرور المؤقتة قبل الوصول إلى نقاط النهاية التشغيلية.",
        [Keys.PermissionDenied] = "لا يمنح دورك صلاحية '{0}' لهذه الوحدة."
    };

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalogs { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SupportedLanguage.English.Code] = English,
            [SupportedLanguage.Urdu.Code] = Urdu,
            [SupportedLanguage.Arabic.Code] = Arabic
        };

    public static IReadOnlyCollection<string> AllKeys => English.Keys.ToList();

    public static string Get(string key, string? language = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var normalized = SupportedLanguage.Normalize(language ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        if (Catalogs.TryGetValue(normalized, out var map) && map.TryGetValue(key, out var localized))
        {
            return localized;
        }

        return English.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string GetFormatted(string key, string? language, params object[] args) =>
        string.Format(SupportedLanguage.ToCultureInfo(language), Get(key, language), args);
}
