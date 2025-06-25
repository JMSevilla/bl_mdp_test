using System;
using System.Collections.Generic;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Domain.Members.Beneficiaries;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class MemberBuilder
{
    private string _referenceNumber = "0003994";
    private string _businessGroup = "RBS";
    private string _title = "Mrs.";
    private string _gender = "F";
    private string _forenames = "forenames";
    private string _surname = "surname";
    private string _schemeCode = "schemeCode";
    private string _insuranceNumber = "insuranceNumber";
    private string _category = "category";
    private MemberStatus _status = MemberStatus.Active;
    private DateTimeOffset? _dateOfBirth = new DateTimeOffset(new DateTime(1985, 10, 10));
    private string _baseCurrency = "baseCurrency";
    private string _name = "name";
    private string _schemeType = "DB";
    private int _normalRetirementAge = 60;
    private int _minimumPensionAge = 60;
    private string _membershipNumber = "memberNumber";
    private string _payrollNumber = "payrollNumber";
    private DateTimeOffset _dateJoinedScheme = new DateTimeOffset(new DateTime(1985, 10, 10));
    private DateTimeOffset _dateLeftScheme = new DateTimeOffset(new DateTime(1985, 10, 10));
    private string _employerCode = "employerCode";
    private string _locationCode = "locationCode";
    private EmailView _emailView = new EmailView(Email.Create("test@gmail.com").Right());
    private string _statusCode = "testStatusCode";
    private string _complaintInticator = "testcomplaintInticator";
    private string _recordsIndicator = null;
    private DateTimeOffset? _datePensionableServiceStarted = null;
    private DateTimeOffset? _dateJoinedCompany = null;
    private readonly List<PaperRetirementApplication> _paperRetirementApplications = new();
    private readonly List<LinkedMember> _linkedMembers = new();
    private readonly List<Beneficiary> _beneficiaries = new();
    private readonly List<BankAccount> _bankAccounts = new();
    private readonly List<ContactReference> _contactReferences = new();

    public Member Build()
    {
        var categoryDetail = new CategoryDetail(_normalRetirementAge, _minimumPensionAge);
        var scheme = new Scheme(_baseCurrency, _name, _schemeType);
        var personalDetails = PersonalDetails.Create(_title, _gender, _forenames, _surname, _dateOfBirth).Right();
        return new(personalDetails, _status, _schemeCode, _businessGroup, _referenceNumber, _category,
            categoryDetail, scheme, _insuranceNumber, _membershipNumber, _payrollNumber, _dateJoinedScheme,
            _dateLeftScheme, _employerCode, _locationCode, _emailView, _statusCode, _complaintInticator,
            _datePensionableServiceStarted, _dateJoinedCompany,
            _paperRetirementApplications, _linkedMembers, _beneficiaries, _bankAccounts, _contactReferences, _recordsIndicator);
    }

    public MemberBuilder PaperRetirementApplications(params PaperRetirementApplication[] paperRetirementApplications)
    {
        foreach (var application in paperRetirementApplications)
            _paperRetirementApplications.Add(application);
        return this;
    }

    public MemberBuilder LinkedMembers(params LinkedMember[] linkedMembers)
    {
        foreach (var linkedMember in linkedMembers)
            _linkedMembers.Add(linkedMember);
        return this;
    }

    public MemberBuilder NormalRetirementAge(int normalRetirementAge)
    {
        _normalRetirementAge = normalRetirementAge;
        return this;
    }

    public MemberBuilder MinimumPensionAge(int minimumPensionAge)
    {
        _minimumPensionAge = minimumPensionAge;
        return this;
    }

    public MemberBuilder DateOfBirth(DateTimeOffset? dateOfBirth)
    {
        _dateOfBirth = dateOfBirth;
        return this;
    }

    public MemberBuilder Gender(string? gender)
    {
        _gender = gender;
        return this;
    }

    public MemberBuilder Status(MemberStatus status)
    {
        _status = status;
        return this;
    }

    public MemberBuilder EmployerCode(string employerCode)
    {
        _employerCode = employerCode;
        return this;
    }

    public MemberBuilder LocationCode(string locationCode)
    {
        _locationCode = locationCode;
        return this;
    }

    public MemberBuilder EmailView(Email email)
    {
        _emailView = email != null ? new EmailView(email) : null;
        return this;
    }

    public MemberBuilder BusinessGroup(string businessGroup)
    {
        _businessGroup = businessGroup;
        return this;
    }

    public MemberBuilder Category(string category)
    {
        _category = category;
        return this;
    }

    public MemberBuilder SchemeType(string schemeType)
    {
        _schemeType = schemeType;
        return this;
    }

    public MemberBuilder SchemeCode(string schemeCode)
    {
        _schemeCode = schemeCode;
        return this;
    }

    public MemberBuilder AddBeneficiary(Beneficiary beneficiary)
    {
        _beneficiaries.Add(beneficiary);
        return this;
    }

    public MemberBuilder AddBankAccount(BankAccount account)
    {
        _bankAccounts.Add(account);
        return this;
    }

    public MemberBuilder AddContactReference()
    {
        var address = Address.Create("Towers Watson Westgate",
               "122-130 Station Road",
               "test dada for addres3",
               "Redhill",
               "Surrey",
               "United Kingdom",
               "GB",
               "RH1 1WS").Right();
        var now = DateTimeOffset.UtcNow;
        var email = Email.Create("test@test.com").Right();
        var mobilePhone = Phone.Create("44 7544111111").Right();
        var contact = new Contact(address, email, mobilePhone, Contact.DataToCopy.Empty(), "RBS", 3141271);
        var authorization = new WTW.MdpService.Domain.Members.Authorization("RBS", "0003994", 1171111, now);
        _contactReferences.Add(new ContactReference(contact, authorization, "RBS", "0003994", 1, now));
        return this;
    }

    public MemberBuilder RecordsIndicator(string recordsIndicator)
    {
        _recordsIndicator = recordsIndicator;
        return this;
    }

    public MemberBuilder DatePensionServiceStarted(DateTimeOffset datePensionServiceStarted)
    {
        _datePensionableServiceStarted = datePensionServiceStarted;
        return this;
    }

    public MemberBuilder DateJoinedCompany(DateTimeOffset dateJoinedCompany)
    {
        _dateJoinedCompany = dateJoinedCompany;
        return this;
    }
    public MemberBuilder Name(string firstName, string lastName)
    {
        _forenames = firstName;
        _surname = lastName;
        return this;
    }
}