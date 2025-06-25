namespace WTW.MdpService.Test.Infrastructure.Content;

public class ContentTestData
{
  public const string ContentBlockList1 = @"[
  {
    ""elements"": {
      ""content"": {
        ""elementType"": ""formattedtext"",
        ""value"": ""While you were an active member of the Fund""
      },
      ""contentBlockKey"": {
        ""elementType"": ""text"",
        ""value"": ""gmp_explanation""
      },
      ""dataSourceUrl"": {
        ""elementType"": ""text"",
        ""value"": ""https://apim.awstas.net/mdp-api/api/retirement/cms-token-information""
      },
      ""errorContent"": {
        ""elementType"": ""formattedtext""
      },
      ""header"": {
        ""elementType"": ""text"",
        ""value"": ""Guaranteed Minimum Pension (GMP)""
      },
      ""headerLink"": {
        ""elementType"": ""text"",
        ""value"": """"
      },
      ""showInAccordion"": {
        ""elementType"": ""optionselection"",
        ""value"": {
          ""label"": ""Closed"",
          ""selection"": ""Closed""
        }
      },
      ""subHeader"": {
        ""elementType"": ""text""
      },
      ""themeColorForBackround"": {
        ""elementType"": ""optionselection""
      }
    },
    ""type"": ""Content HTML block""
  }
]";

  public const string ContentBlockList2 = @"[
  {
    ""elements"": {
      ""content"": {
        ""elementType"": ""formattedtext"",
        ""value"": ""Pension: this is the pension income your spouse""
      },
      ""contentBlockKey"": {
        ""elementType"": ""text"",
        ""value"": ""view_death_benefits""
      },
      ""dataSourceUrl"": {
        ""elementType"": ""text""
      },
      ""errorContent"": {
        ""elementType"": ""formattedtext"",
        ""value"": """"
      },
      ""header"": {
        ""elementType"": ""text"",
        ""value"": ""Death after retirement – what happens?""
      },
      ""headerLink"": {
        ""elementType"": ""text"",
        ""value"": """"
      },
      ""showInAccordion"": {
        ""elementType"": ""optionselection""
      },
      ""subHeader"": {
        ""elementType"": ""text""
      },
      ""themeColorForBackround"": {
        ""elementType"": ""optionselection""
      }
    },
    ""type"": ""Content HTML block""
  }
]";

  public const string SummaryItemsList = @"
  {
      ""summaryItems"": {
        ""values"": [
          {
            ""elements"": {
              ""description"": {
                ""elementType"": ""text"",
                ""value"": ""When you reach your State Pension Age""
              },
              ""divider"": {
                ""elementType"": ""toggle"",
                ""value"": false
              },
              ""explanationSummaryItems"": {
                ""values"": [
                  {
                    ""elements"": {
                      ""boldValue"": {
                        ""elementType"": ""toggle""
                      },
                      ""callToAction"": {
                        ""elementType"": ""reference""
                      },
                      ""description"": {
                        ""elementType"": ""text"",
                        ""value"": ""[[label:tranche_pre88GMP_*]]""
                      },
                      ""divider"": {
                        ""elementType"": ""toggle"",
                        ""value"": true
                      },
                      ""explanationSummaryItems"": {
                        ""elementType"": ""reference""
                      },
                      ""format"": {
                        ""elementType"": ""optionselection"",
                        ""value"": {
                          ""label"": ""Currency per year"",
                          ""selection"": ""Currency per year""
                        }
                      },
                      ""header"": {
                        ""elementType"": ""text"",
                        ""value"": ""Guaranteed Minimum Pension accrued before 6 April 1988""
                      },
                      ""link"": {
                        ""elementType"": ""text""
                      },
                      ""linkText"": {
                        ""elementType"": ""text""
                      },
                      ""showDespiteEmpty"": {
                        ""elementType"": ""toggle""
                      },
                      ""slimValueStyle"": {
                        ""elementType"": ""toggle""
                      },
                      ""tooltip"": {
                        ""elementType"": ""reference""
                      },
                      ""value"": {
                        ""elementType"": ""text"",
                        ""value"": ""fullPensionDCAsLumpSum.pensionTranches.pre88GMP""
                      }
                    },
                    ""type"": ""Summary item""
                  }
                ]
              },
              ""format"": {
                ""elementType"": ""optionselection"",
                ""value"": {
                  ""label"": ""Currency per year"",
                  ""selection"": ""Currency per year""
                }
              },
              ""header"": {
                ""elementType"": ""text"",
                ""value"": ""Pension income""
              },
              ""link"": {
                ""elementType"": ""text"",
                ""value"": """"
              },
              ""linkText"": {
                ""elementType"": ""text"",
                ""value"": """"
              },
              ""tooltip"": {
                ""elementType"": ""reference""
              },
              ""value"": {
                ""elementType"": ""text"",
                ""value"": ""fullPensionDCAsLumpSum.totalPension""
              }
            },
            ""type"": ""Summary item""
          },
          {
            ""elements"": {
              ""description"": {
                ""elementType"": ""text"",
                ""value"": ""[[badge:PROTECTED UNTIL [[data-date:quoteExpiryDate]]]]""
              },
              ""divider"": {
                ""elementType"": ""toggle"",
                ""value"": false
              },
              ""explanationSummaryItems"": {},
              ""format"": {
                ""elementType"": ""optionselection"",
                ""value"": {
                  ""label"": ""Currency per year"",
                  ""selection"": ""Currency per year""
                }
              },
              ""header"": {
                ""elementType"": ""text"",
                ""value"": ""Pension income""
              },
              ""link"": {
                ""elementType"": ""text"",
                ""value"": """"
              },
              ""linkText"": {
                ""elementType"": ""text"",
                ""value"": """"
              },
              ""tooltip"": {
                ""elementType"": ""reference""
              },
              ""value"": {
                ""elementType"": ""text"",
                ""value"": ""fullPensionDCAsLumpSum.totalPension""
              }
            },
            ""type"": ""Summary item""
          }
        ]
      }
    }";
}
