using LanguageExt;
using LanguageExt.Common;
using WTW.MdpService.Domain;

namespace WTW.MdpService.Test.Domain;

public class PhoneBuilder
{
    public string _code = "44";
    public string _number = "7544111111";
    public string _fullNumber = "447544111111";

    public Either<Error, Phone> Build()
    {
        return Phone.Create(_code, _number);
    }

    public Either<Error, Phone> BuildFull()
    {
        return Phone.Create(_fullNumber);
    }

    public PhoneBuilder Code(string code)
    {
        _code = code;
        return this;
    }

    public PhoneBuilder Number(string number) 
    {
        _number = number;
        return this;
    }

    public PhoneBuilder FullNumber(string fullNumber) 
    {
        _fullNumber = fullNumber;
        return this;
    }
}