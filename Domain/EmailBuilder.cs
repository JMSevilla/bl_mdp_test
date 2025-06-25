using LanguageExt;
using LanguageExt.Common;
using WTW.MdpService.Domain;

namespace WTW.MdpService.Test.Domain;

public class EmailBuilder
{
    public string _email = "test@test.com";

    public EmailBuilder EmailAddress(string email)
    {
        _email = email;
        return this;
    }

    public Either<Error, Email> Build()
    {
        return Email.Create(_email);
    }
}