using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.ApplyFinancials;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.ApplyFinancials;

public class ApplyFinancialsClientTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ApplyFinancialsClient _sut;

    public ApplyFinancialsClientTest()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        _sut = new ApplyFinancialsClient(_httpClient, "testUser", "testPassword");
    }

    public async Task ThrowsInvalidOperationException_WhenInvalidLoginTokenIsReceived()
    {
        var expectedResponse = GetAccountValidationResponse();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"\"}")
            });

        var action = async () => await _sut.ValidateIbanBankAccount("testIban", "testBic", "testCountryCode");

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Apply Finance API: Username and password do not match.");
    }

    public async Task ValidateIbanBankAccount_Success_ReturnsAccountValidationResponse()
    {
        var expectedResponse = GetAccountValidationResponse();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var result = await _sut.ValidateIbanBankAccount("testIban", "testBic", "testCountryCode");

        result.IsRight.Should().BeTrue();
        AssetResponseValues(expectedResponse, result.Right());
    }

    [Input("FAIL")]
    [Input("CAUTION")]
    public async Task ValidateIbanBankAccount_Failure_ReturnsError(string status)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"{{\"Status\":\"{status}\"}}")
            });

        var result = await _sut.ValidateIbanBankAccount("testIban", "testBic", "testCountryCode");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("failed_external_bank_details_validation");
    }

    public async Task ValidateIbanBankAccount_HttpRequestException_ReturnsError()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Test exception"));

        var result = await _sut.ValidateIbanBankAccount("testIban", "testBic", "testCountryCode");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("access_to_external_bank_details_failed");
    }

    [Input(HttpStatusCode.BadRequest, "testMessage")]
    [Input(HttpStatusCode.RedirectKeepVerb, "No such host is known.")]
    public async Task ValidateIbanBankAccount_ThrowsWhenConfigurationFailed(HttpStatusCode httpStatusCode, string message)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException(message, new Exception(), httpStatusCode));

        var action = async () => await _sut.ValidateIbanBankAccount("testIban", "testBic", "testCountryCode");

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task ValidateUkBankAccount_Success_ReturnsAccountValidationResponse()
    {
        var expectedResponse = GetAccountValidationResponse();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var result = await _sut.ValidateUkBankAccount("testAccountNumber", "testSortCode");

        result.IsRight.Should().BeTrue();
        AssetResponseValues(expectedResponse, result.Right());
    }

    [Input("FAIL")]
    [Input("CAUTION")]
    public async Task ValidateUkBankAccount_Failure_ReturnsError(string status)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"{{\"Status\":\"{status}\"}}")
            });

        var result = await _sut.ValidateUkBankAccount("testAccountNumber", "testSortCode");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("failed_external_bank_details_validation");
    }

    public async Task ValidateUkBankAccount_HttpRequestException_ReturnsError()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Test exception"));

        var result = await _sut.ValidateUkBankAccount("testAccountNumber", "testSortCode");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("access_to_external_bank_details_failed");
    }

    [Input(HttpStatusCode.BadRequest, "testMessage")]
    [Input(HttpStatusCode.RedirectKeepVerb, "No such host is known.")]
    public async Task ValidateUkBankAccount_ThrowsWhenConfigurationFailed(HttpStatusCode httpStatusCode, string message)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/authenticate")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Token\":\"testToken\"}")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/convert")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException(message, new Exception(), httpStatusCode));

        var action = async () => await _sut.ValidateUkBankAccount("testAccountNumber", "testSortCode");

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    private static void AssetResponseValues(AccountValidationResponse expectedResponse, AccountValidationResponse response)
    {
        response.CountryCode.Should().Be(expectedResponse.CountryCode);
        response.NationalId.Should().Be(expectedResponse.NationalId);
        response.AccountNumber.Should().Be(expectedResponse.AccountNumber);
        response.Status.Should().Be(expectedResponse.Status);
        response.Comment.Should().Be(expectedResponse.Comment);
        response.RecommendedNatId.Should().Be(expectedResponse.RecommendedNatId);
        response.RecommendedAcct.Should().Be(expectedResponse.RecommendedAcct);
        response.RecommendedBIC.Should().Be(expectedResponse.RecommendedBIC);
        response.Ref.Should().Be(expectedResponse.Ref);
        response.Group.Should().Be(expectedResponse.Group);
        response.Bic8.Should().Be(expectedResponse.Bic8);
        response.DataStore.Should().Be(expectedResponse.DataStore);
        response.NoBranch.Should().Be(expectedResponse.NoBranch);
        response.IsoAddr.Should().Be(expectedResponse.IsoAddr);
        response.PayBranchType.Should().Be(expectedResponse.PayBranchType);
        response.FreeToken.Should().Be(expectedResponse.FreeToken);

        response.PaymentBicDetails.BranchTypeLabel.Should().Be(expectedResponse.PaymentBicDetails.BranchTypeLabel);
        response.PaymentBicDetails.CodeDetails.CodeName1.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeName1);
        response.PaymentBicDetails.CodeDetails.CodeName2.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeName2);
        response.PaymentBicDetails.CodeDetails.CodeName3.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeName3);
        response.PaymentBicDetails.CodeDetails.CodeName4.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeName4);
        response.PaymentBicDetails.CodeDetails.CodeValue1.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeValue1);
        response.PaymentBicDetails.CodeDetails.CodeValue2.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeValue2);
        response.PaymentBicDetails.CodeDetails.CodeValue3.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeValue3);
        response.PaymentBicDetails.CodeDetails.CodeValue4.Should().Be(expectedResponse.PaymentBicDetails.CodeDetails.CodeValue4);
        response.PaymentBicDetails.AdditionalData.SsiAvailable.Should().Be(expectedResponse.PaymentBicDetails.AdditionalData.SsiAvailable);
        response.PaymentBicDetails.AdditionalData.PayServiceAvailable.Should().Be(expectedResponse.PaymentBicDetails.AdditionalData.PayServiceAvailable);
        response.PaymentBicDetails.AdditionalData.ContactsAvailable.Should().Be(expectedResponse.PaymentBicDetails.AdditionalData.ContactsAvailable);
        response.PaymentBicDetails.AdditionalData.MessageAvailable.Should().Be(expectedResponse.PaymentBicDetails.AdditionalData.MessageAvailable);
        response.PaymentBicDetails.AdditionalData.HolidayAvailable.Should().Be(expectedResponse.PaymentBicDetails.AdditionalData.HolidayAvailable);
        response.PaymentBicDetails.BankName.Should().Be(expectedResponse.PaymentBicDetails.BankName);
        response.PaymentBicDetails.Branch.Should().Be(expectedResponse.PaymentBicDetails.Branch);
        response.PaymentBicDetails.Street.Should().Be(expectedResponse.PaymentBicDetails.Street);
        response.PaymentBicDetails.City.Should().Be(expectedResponse.PaymentBicDetails.City);
        response.PaymentBicDetails.PostZip.Should().Be(expectedResponse.PaymentBicDetails.PostZip);
        response.PaymentBicDetails.Region.Should().Be(expectedResponse.PaymentBicDetails.Region);
        response.PaymentBicDetails.Country.Should().Be(expectedResponse.PaymentBicDetails.Country);

        response.HeadOfficeDetails.CodeDetails.CodeName1.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeName1);
        response.HeadOfficeDetails.CodeDetails.CodeName2.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeName2);
        response.HeadOfficeDetails.CodeDetails.CodeName3.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeName3);
        response.HeadOfficeDetails.CodeDetails.CodeName4.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeName4);
        response.HeadOfficeDetails.CodeDetails.CodeValue1.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeValue1);
        response.HeadOfficeDetails.CodeDetails.CodeValue2.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeValue2);
        response.HeadOfficeDetails.CodeDetails.CodeValue3.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeValue3);
        response.HeadOfficeDetails.CodeDetails.CodeValue4.Should().Be(expectedResponse.HeadOfficeDetails.CodeDetails.CodeValue4);
        response.HeadOfficeDetails.AdditionalData.SsiAvailable.Should().Be(expectedResponse.HeadOfficeDetails.AdditionalData.SsiAvailable);
        response.HeadOfficeDetails.AdditionalData.PayServiceAvailable.Should().Be(expectedResponse.HeadOfficeDetails.AdditionalData.PayServiceAvailable);
        response.HeadOfficeDetails.AdditionalData.ContactsAvailable.Should().Be(expectedResponse.HeadOfficeDetails.AdditionalData.ContactsAvailable);
        response.HeadOfficeDetails.AdditionalData.MessageAvailable.Should().Be(expectedResponse.HeadOfficeDetails.AdditionalData.MessageAvailable);
        response.HeadOfficeDetails.AdditionalData.HolidayAvailable.Should().Be(expectedResponse.HeadOfficeDetails.AdditionalData.HolidayAvailable);
        response.HeadOfficeDetails.BankName.Should().Be(expectedResponse.HeadOfficeDetails.BankName);
        response.HeadOfficeDetails.Branch.Should().Be(expectedResponse.HeadOfficeDetails.Branch);
        response.HeadOfficeDetails.Street.Should().Be(expectedResponse.HeadOfficeDetails.Street);
        response.HeadOfficeDetails.City.Should().Be(expectedResponse.HeadOfficeDetails.City);
        response.HeadOfficeDetails.PostZip.Should().Be(expectedResponse.HeadOfficeDetails.PostZip);
        response.HeadOfficeDetails.Region.Should().Be(expectedResponse.HeadOfficeDetails.Region);
        response.HeadOfficeDetails.Country.Should().Be(expectedResponse.HeadOfficeDetails.Country);

        response.BranchDetails.Single().CodeDetails.CodeName1.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeName1);
        response.BranchDetails.Single().CodeDetails.CodeName2.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeName2);
        response.BranchDetails.Single().CodeDetails.CodeName3.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeName3);
        response.BranchDetails.Single().CodeDetails.CodeName4.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeName4);
        response.BranchDetails.Single().CodeDetails.CodeValue1.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeValue1);
        response.BranchDetails.Single().CodeDetails.CodeValue2.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeValue2);
        response.BranchDetails.Single().CodeDetails.CodeValue3.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeValue3);
        response.BranchDetails.Single().CodeDetails.CodeValue4.Should().Be(expectedResponse.BranchDetails.Single().CodeDetails.CodeValue4);
        response.BranchDetails.Single().SepaDetails.CtStatus.Should().Be(expectedResponse.BranchDetails.Single().SepaDetails.CtStatus);
        response.BranchDetails.Single().SepaDetails.DdStatus.Should().Be(expectedResponse.BranchDetails.Single().SepaDetails.DdStatus);
        response.BranchDetails.Single().SepaDetails.BbStatus.Should().Be(expectedResponse.BranchDetails.Single().SepaDetails.BbStatus);
        response.BranchDetails.Single().AdditionalData.SsiAvailable.Should().Be(expectedResponse.BranchDetails.Single().AdditionalData.SsiAvailable);
        response.BranchDetails.Single().AdditionalData.PayServiceAvailable.Should().Be(expectedResponse.BranchDetails.Single().AdditionalData.PayServiceAvailable);
        response.BranchDetails.Single().AdditionalData.ContactsAvailable.Should().Be(expectedResponse.BranchDetails.Single().AdditionalData.ContactsAvailable);
        response.BranchDetails.Single().AdditionalData.MessageAvailable.Should().Be(expectedResponse.BranchDetails.Single().AdditionalData.MessageAvailable);
        response.BranchDetails.Single().AdditionalData.HolidayAvailable.Should().Be(expectedResponse.BranchDetails.Single().AdditionalData.HolidayAvailable);
        response.BranchDetails.Single().BankToken.Should().Be(expectedResponse.BranchDetails.Single().BankToken);
        response.BranchDetails.Single().BankName.Should().Be(expectedResponse.BranchDetails.Single().BankName);
        response.BranchDetails.Single().Branch.Should().Be(expectedResponse.BranchDetails.Single().Branch);
        response.BranchDetails.Single().Street.Should().Be(expectedResponse.BranchDetails.Single().Street);
        response.BranchDetails.Single().City.Should().Be(expectedResponse.BranchDetails.Single().City);
        response.BranchDetails.Single().PostZip.Should().Be(expectedResponse.BranchDetails.Single().PostZip);
        response.BranchDetails.Single().Region.Should().Be(expectedResponse.BranchDetails.Single().Region);
        response.BranchDetails.Single().Country.Should().Be(expectedResponse.BranchDetails.Single().Country);
    }

    private static AccountValidationResponse GetAccountValidationResponse()
    {
        return new AccountValidationResponse
        {
            CountryCode = "US",
            NationalId = "1234567890",
            AccountNumber = "123456789012",
            Status = "SUCCESS",
            Comment = "Test Comment",
            RecommendedNatId = "9876543210",
            RecommendedAcct = "987654321012",
            RecommendedBIC = "BICCODE8",
            Ref = "REF123456789",
            Group = "Group1",
            BranchDetails = new List<BranchDetail>
            {
                new BranchDetail
                {
                    BankName = "Bank1",
                    Branch = "Branch1",
                    Street = "1234 Test St",
                    City = "TestCity",
                    PostZip = "12345",
                    Region = "Region1",
                    Country = "US",
                    CodeDetails = new CodeDetails
                    {
                        CodeName1 = "CodeName1",
                        CodeValue1 = "CodeValue1",
                        CodeName2 = "CodeName2",
                        CodeValue2 = "CodeValue2",
                        CodeName3 = "CodeName3",
                        CodeValue3 = "CodeValue3",
                        CodeName4 = "CodeName4",
                        CodeValue4 = "CodeValue4"
                    },
                    SepaDetails = new SepaDetails
                    {
                        CtStatus = "CTStatus1",
                        DdStatus = "DDStatus1",
                        BbStatus = "BBStatus1"
                    },
                    AdditionalData = new AdditionalData
                    {
                        SsiAvailable = "Yes",
                        PayServiceAvailable = "Yes",
                        ContactsAvailable = "Yes",
                        MessageAvailable = "Yes",
                        HolidayAvailable = "Yes"
                    },
                    BankToken = "BankToken1"
                }
            },
            HeadOfficeDetails = new HeadOfficeDetails
            {
                BankName = "HeadOfficeBank",
                Branch = "MainBranch",
                Street = "5678 Main St",
                City = "MainCity",
                PostZip = "67890",
                Region = "Region2",
                Country = "US",
                CodeDetails = new CodeDetails
                {
                    CodeName1 = "CodeName1",
                    CodeValue1 = "CodeValue1",
                    CodeName2 = "CodeName2",
                    CodeValue2 = "CodeValue2",
                    CodeName3 = "CodeName3",
                    CodeValue3 = "CodeValue3",
                    CodeName4 = "CodeName4",
                    CodeValue4 = "CodeValue4"
                },
                AdditionalData = new AdditionalData
                {
                    SsiAvailable = "Yes",
                    PayServiceAvailable = "Yes",
                    ContactsAvailable = "Yes",
                    MessageAvailable = "Yes",
                    HolidayAvailable = "Yes"
                }
            },
            PaymentBicDetails = new PaymentBicDetails
            {
                BankName = "PaymentBank",
                Branch = "PaymentBranch",
                Street = "91011 Payment St",
                City = "PaymentCity",
                PostZip = "11213",
                Region = "Region3",
                Country = "US",
                BranchTypeLabel = "BranchType1",
                CodeDetails = new CodeDetails
                {
                    CodeName1 = "CodeName1",
                    CodeValue1 = "CodeValue1",
                    CodeName2 = "CodeName2",
                    CodeValue2 = "CodeValue2",
                    CodeName3 = "CodeName3",
                    CodeValue3 = "CodeValue3",
                    CodeName4 = "CodeName4",
                    CodeValue4 = "CodeValue4"
                },
                AdditionalData = new AdditionalData
                {
                    SsiAvailable = "Yes",
                    PayServiceAvailable = "Yes",
                    ContactsAvailable = "Yes",
                    MessageAvailable = "Yes",
                    HolidayAvailable = "Yes"
                }
            },
            Bic8 = "BICCODE8",
            DataStore = "DataStore1",
            NoBranch = "NoBranch1",
            IsoAddr = "IsoAddress1",
            PayBranchType = "Type1",
            FreeToken = "FreeToken1"
        };
        ;
    }
}
