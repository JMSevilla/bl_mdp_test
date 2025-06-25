namespace WTW.MdpService.Test.Infrastructure.Investment;

public record InvestmentTestData
{
    public const string BalanceResponseJson = @"{
  ""currency"": ""GBP"",
  ""totalPaidIn"": 33045.64,
  ""totalValue"": 32642.63,
  ""funds"": [
    {
      ""code"": ""LSBO"",
      ""name"": null,
      ""value"": 4899.07
    },
    {
      ""code"": ""LSCA"",
      ""name"": null,
      ""value"": 14273.28
    },
    {
      ""code"": ""LSDG"",
      ""name"": null,
      ""value"": 13470.28
    }
  ],
  ""contributions"": [
    {
      ""code"": ""HIST"",
      ""name"": ""Bulk transfer"",
      ""paidIn"": 33045.64,
      ""value"": 32642.63
    }
  ]
}";

    public const string ForecastResponseJson = @"{
  ""ages"": [
    {
      ""age"": 64,
      ""assetValue"": 195982.88,
      ""taxFreeCash"": 48995.72
    },
    {
      ""age"": 65,
      ""assetValue"": 297411.28,
      ""taxFreeCash"": 74352.82
    },
    {
      ""age"": 66,
      ""assetValue"": 444908.03,
      ""taxFreeCash"": 111227.01
    }
  ]
}";

    public const string StrategiesResponseJson = @"{
  ""contributionTypes"": [
    {
      ""contributionType"": ""LAVC"",
      ""strategies"": [
        {
          ""code"": ""GSKLIFCY"",
          ""name"": ""CH Drawdown Lifecycle""
        },
        {
          ""code"": ""LIFDCASH"",
          ""name"": ""Higher Risk Cash""
        }
      ]
    },
    {
      ""contributionType"": ""MBFA"",
      ""strategies"": [
        {
          ""code"": ""GSKLIFCY"",
          ""name"": ""CH Drawdown Lifecycle""
        },
        {
          ""code"": ""LIFFCORE"",
          ""name"": ""Lower Risk Annuity""
        }
      ]
    }
  ]
}";

    public const string FundsResponseJson = @"{
  ""contributionTypes"": [
    {
      ""contributionType"": ""SPEN"",
      ""funds"": [
        {
          ""code"": ""LIFE"",
          ""name"": ""LifeSight Equity"",
          ""annualMemberFee"": 0.4105
        },
        {
          ""code"": ""CLIF"",
          ""name"": ""Climate Focus"",
          ""annualMemberFee"": 0.3702
        }
      ]
    },
    {
      ""contributionType"": ""TVIN"",
      ""funds"": [
        {
          ""code"": ""LMBO"",
          ""name"": ""LifeSight Bonds"",
          ""annualMemberFee"": 0.4167
        },
        {
          ""code"": ""LMCA"",
          ""name"": ""Lifesight Cash"",
          ""annualMemberFee"": 0.4015
        }
      ]
    }
  ]
}";
    public const string TargetSchemeMappingResponseJson = @"{
  ""targetBusinessGroup"": ""LIF"",
  ""targetSchemeCode"": ""9000"",
  ""targetCategoryCode"": ""9000"",
  ""targetStatus"": ""PP"",
  ""targetContributionType"": ""SPEN""
}";

    public const string LatestContributionJson = @"{
    ""TotalValue"": 383.97,
    ""Currency"": ""GBP"",
    ""PaymentDate"": ""2024-10-09"",
    ""ContributionsList"": [
        {
            ""Name"": ""Employee contribution"",
            ""ContributionValue"": 153.59,
            ""RegPayDate"":""2024-10-09""
        },
        {
            ""Name"": ""Employer contribution"",
            ""ContributionValue"": 230.38,
            ""RegPayDate"":""2024-10-10""
        }
    ]
}";

    public const string ThreeContributionTypesNonZeroValueJson = @"{
    ""TotalValue"": 383.97,
    ""Currency"": ""GBP"",
    ""PaymentDate"": ""2024-10-09"",
    ""ContributionsList"": [
        {
            ""Name"": ""Employee contribution 1"",
            ""ContributionValue"": 153.59,
            ""RegPayDate"":""2024-10-09""
        },
        {
            ""Name"": ""Employer contribution 2"",
            ""ContributionValue"": 230.38,
            ""RegPayDate"":""2024-10-10""
        },
        {
            ""Name"": ""Employer contribution 3"",
            ""ContributionValue"": 240.38,
            ""RegPayDate"":""2024-10-11""
        }
    ]
}";

    public const string ThreeContributionTypesWithZeroValueJson = @"{
    ""TotalValue"": 383.97,
    ""Currency"": ""GBP"",
    ""PaymentDate"": ""2024-10-09"",
    ""ContributionsList"": [
        {
            ""Name"": ""Employee contribution 1"",
            ""ContributionValue"": 153.59,
            ""RegPayDate"":""2024-10-09""
        },
        {
            ""Name"": ""Employer contribution 2"",
            ""ContributionValue"": 230.38,
            ""RegPayDate"":""2024-10-10""
        },
        {
            ""Name"": ""Employer contribution 3"",
            ""ContributionValue"": 0,
            ""RegPayDate"":""2024-10-11""
        }
    ]
}";
}
