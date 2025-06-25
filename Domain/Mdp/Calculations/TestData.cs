namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public record TestData
{
    public const string RetiremntJson = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1962-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y2M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y3M"",""quotes"":[{""name"":""FullPension"",""sequenceNumber"":1,""lumpSumFromDb"":null,""lumpSumFromDc"":null,""smallPotLumpSum"":null,""taxFreeUfpls"":null,""taxableUfpls"":null,""totalLumpSum"":null,""totalPension"":32369.4,""totalSpousePension"":12247.2,""totalUfpls"":null,""transferValueOfDc"":null,""trivialCommutationLumpSum"":null,""annuityPurchaseAmount"":null,""minimumLumpSum"":null,""maximumLumpSum"":null,""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""ReducedPension"",""sequenceNumber"":2,""lumpSumFromDb"":165346.35,""lumpSumFromDc"":null,""smallPotLumpSum"":null,""taxFreeUfpls"":null,""taxableUfpls"":null,""totalLumpSum"":165346.35,""totalPension"":24801.96,""totalSpousePension"":12247.2,""totalUfpls"":null,""transferValueOfDc"":null,""trivialCommutationLumpSum"":null,""annuityPurchaseAmount"":null,""minimumLumpSum"":100.0,""maximumLumpSum"":165346.35,""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88""]}";
    public const string RetiremntJsonNew = @"{""calculationEventType "":""PV"",""dateOfBirth"":""1964-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":null,""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.1,""totalAvcFundValue"":0.1,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":null,""quotes"":[{""name"":""FullPension"",""sequenceNumber"":1,""lumpSumFromDb"":null,""lumpSumFromDc"":null,""smallPotLumpSum"":null,""taxFreeUfpls"":null,""taxableUfpls"":null,""totalLumpSum"":null,""totalPension"":32369.4,""totalSpousePension"":12247.2,""totalUfpls"":null,""transferValueOfDc"":null,""trivialCommutationLumpSum"":null,""annuityPurchaseAmount"":null,""minimumLumpSum"":null,""maximumLumpSum"":null,""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""ReducedPension"",""sequenceNumber"":2,""lumpSumFromDb"":165346.35,""lumpSumFromDc"":null,""smallPotLumpSum"":null,""taxFreeUfpls"":null,""taxableUfpls"":null,""totalLumpSum"":165346.35,""totalPension"":24801.96,""totalSpousePension"":12247.2,""totalUfpls"":null,""transferValueOfDc"":null,""trivialCommutationLumpSum"":null,""annuityPurchaseAmount"":null,""minimumLumpSum"":100.0,""maximumLumpSum"":165346.35,""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]},{""name"":""FullPensionDCAsUFPLS"",""sequenceNumber"":2,""lumpSumFromDb"":165346.35,""lumpSumFromDc"":null,""smallPotLumpSum"":null,""taxFreeUfpls"":null,""taxableUfpls"":null,""totalLumpSum"":165346.35,""totalPension"":24801.96,""totalSpousePension"":12247.2,""totalUfpls"":null,""transferValueOfDc"":null,""trivialCommutationLumpSum"":null,""annuityPurchaseAmount"":null,""minimumLumpSum"":100.0,""maximumLumpSum"":165346.35,""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88""]}";

    public const string RetirementV2ReducedPensionNoLumpSumJson = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1962-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""totalFundValue"":null,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""minimumPermittedTotalLumpSum"":0,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalLTAUsedPerc"",""value"":14.72},{""name"":""totalPension"",""value"":32369.4},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""reducedPension"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":165346.35},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""maximumPermittedTotalLumpSum"",""value"":165346.35},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""totalLTAUsedPerc"",""value"":16.03},{""name"":""totalLumpSumBadData"",""value"":165346.35},{""name"":""totalPension"",""value"":24801.96},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88"",""GMPEQCHECKED"",""tranche_post88GMP_GMP"",""tranche_post97_LPI5"",""tranche_pre88GMP_NIL"",""tranche_pre97Excess_LPI5""]}";
    public const string SelectedQuoteV2ReducedPensionNoLumpSumDict = @"{ ""reducedPension_totalSpousePension"" : 6387.12 , ""reducedPension_totalPension"" : 9975.12 , ""reducedPension_totalLumpSumBadData"" : 66577.62 , ""reducedPension_totalTaxFreeLumpSum"" : 66577.62 , ""reducedPension_lumpSumFromDB"" : 58111.4 , ""reducedPension_lumpSumFromDC"" : 8466.22 , ""reducedPension_minimumPermittedTotalLumpSum"" : 8566.22 , ""reducedPension_maximumPermittedTotalLumpSum"" : 66577.62 , ""reducedPension_totalLTAUsedPerc"" :  24.81 , ""reducedPension_pensionTranches_post97"" : 9975.12 }";
    public const string RetirementV2ReducedPensionNoMaxPermittedLumpSumJson = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1962-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""totalFundValue"":null,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""minimumPermittedTotalLumpSum"":0,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalLTAUsedPerc"",""value"":14.72},{""name"":""totalPension"",""value"":32369.4},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""reducedPension"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":165346.35},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""maximumPermittedTotalLumpSumBadData"",""value"":165346.35},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""totalLTAUsedPerc"",""value"":16.03},{""name"":""totalLumpSum"",""value"":165346.35},{""name"":""totalPension"",""value"":24801.96},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88"",""GMPEQCHECKED"",""tranche_post88GMP_GMP"",""tranche_post97_LPI5"",""tranche_pre88GMP_NIL"",""tranche_pre97Excess_LPI5""]}";
    public const string SelectedQuoteV2ReducedPensionNoMaxPermittedLumpSumDict = @"{ ""reducedPension_totalSpousePension"" : 6387.12 , ""reducedPension_totalPension"" : 9975.12 , ""reducedPension_totalLumpSum"" : 66577.62 , ""reducedPension_totalTaxFreeLumpSum"" : 66577.62 , ""reducedPension_lumpSumFromDB"" : 58111.4 , ""reducedPension_lumpSumFromDC"" : 8466.22 , ""reducedPension_minimumPermittedTotalLumpSum"" : 8566.22 , ""reducedPension_maximumPermittedTotalLumpSumBadData"" : 66577.62 , ""reducedPension_totalLTAUsedPerc"" :  24.81 , ""reducedPension_pensionTranches_post97"" : 9975.12 }";
    public const string RetirementV2ReducedPensionLumsumSmallerThanMaxPermittedLumpSumJson = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1962-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""totalFundValue"":null,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""minimumPermittedTotalLumpSum"":0,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalLTAUsedPerc"",""value"":14.72},{""name"":""totalPension"",""value"":32369.4},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""reducedPension"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":165346.35},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""maximumPermittedTotalLumpSum"",""value"":165346.35},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""totalLTAUsedPerc"",""value"":16.03},{""name"":""totalLumpSum"",""value"":165.35},{""name"":""totalPension"",""value"":24801.96},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88"",""GMPEQCHECKED"",""tranche_post88GMP_GMP"",""tranche_post97_LPI5"",""tranche_pre88GMP_NIL"",""tranche_pre97Excess_LPI5""]}";
    public const string SelectedQuoteV2ReducedPensionLumsumSmallerThanMaxPermittedLumpSumDict = @"{ ""reducedPension_totalSpousePension"" : 6387.12 , ""reducedPension_totalPension"" : 9975.12 , ""reducedPension_totalLumpSum"" : 665.62 , ""reducedPension_totalTaxFreeLumpSum"" : 66577.62 , ""reducedPension_lumpSumFromDB"" : 58111.4 , ""reducedPension_lumpSumFromDC"" : 8466.22 , ""reducedPension_minimumPermittedTotalLumpSum"" : 8566.22 , ""reducedPension_maximumPermittedTotalLumpSum"" : 66577.62 , ""reducedPension_totalLTAUsedPerc"" :  24.81 , ""reducedPension_pensionTranches_post97"" : 9975.12 }";
    public const string RetirementV2ReducedPensionNestedLumsumSmallerThanMaxPermittedLumpSumJson = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1963-11-04T00:00:00"",""datePensionableServiceCommenced"":""1982-09-21T00:00:00"",""dateOfLeaving"":""2000-09-30T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P65Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":1336.4,""post88GMPAtGMPAge"":3575.52,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":11308.22,""externalAvcFundValue"":0.0,""totalAvcFundValue"":11308.22,""totalFundValue"":null,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":41.61,""minimumPermittedTotalLumpSum"":0,""maximumPermittedTotalLumpSum"":111202.22,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalSpousePension"",""value"":10825.44},{""name"":""totalPension"",""value"":21650.88},{""name"":""totalDCFundValueForChoice"",""value"":11308.22}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":17700.34},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3845.16}]},{""name"":""fullPension_DCAsUFPLS"",""attributes"":[{""name"":""taxableUFPLS"",""value"":8481.16},{""name"":""taxFreeUFPLS"",""value"":2827.06},{""name"":""totalUFPLS"",""value"":11308.22},{""name"":""totalTaxFreeLumpSum"",""value"":2827.06},{""name"":""totalLTAUsedPerc"",""value"":41.4}],""pensionTranches"":[]},{""name"":""fullPension_DCAsPCLS"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":0.0},{""name"":""lumpSumFromDC"",""value"":2827.06},{""name"":""totalLumpSum"",""value"":2827.06},{""name"":""totalTaxFreeLumpSum"",""value"":2827.06},{""name"":""annuityPurchaseAmount"",""value"":8481.16},{""name"":""totalLTAUsedPerc"",""value"":41.4}],""pensionTranches"":[]},{""name"":""fullPension_DCAsOMOAnnuity"",""attributes"":[{""name"":""annuityPurchaseAmount"",""value"":11308.22},{""name"":""totalLTAUsedPerc"",""value"":41.4}],""pensionTranches"":[]},{""name"":""fullPension_DCAsTransfer"",""attributes"":[{""name"":""transferValueOfDC"",""value"":0.0},{""name"":""totalLTAUsedPerc"",""value"":0.0}],""pensionTranches"":[]},{""name"":""reducedPension"",""attributes"":[{""name"":""totalSpousePension"",""value"":10825.44},{""name"":""totalPension"",""value"":16336.03},{""name"":""totalLumpSum"",""value"":108900.16},{""name"":""lumpSumFromDB"",""value"":108900.16},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""totalTaxFreeLumpSum"",""value"":108900.16},{""name"":""totalLTAUsedPerc"",""value"":40.59},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""maximumPermittedTotalLumpSum"",""value"":108905.16},{""name"":""totalDCFundValueForChoice"",""value"":11308.22}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":12806.27},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3424.38}]},{""name"":""reducedPension_DCAsUFPLS"",""attributes"":[{""name"":""taxableUFPLS"",""value"":8481.16},{""name"":""taxFreeUFPLS"",""value"":2827.06},{""name"":""totalUFPLS"",""value"":11308.22},{""name"":""totalLTAUsedPerc"",""value"":41.64},{""name"":""totalTaxFreeLumpSum"",""value"":111727.22},{""name"":""totalLumpSum"",""value"":120208.38},{""name"":""totalPension"",""value"":16336.03}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":12806.27},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3424.38}]},{""name"":""reducedPension_DCAsPCLS"",""attributes"":[{""name"":""lumpSumFromDC"",""value"":2827.06},{""name"":""totalLumpSum"",""value"":111727.22},{""name"":""annuityPurchaseAmount"",""value"":8481.16},{""name"":""totalLTAUsedPerc"",""value"":41.64},{""name"":""totalTaxFreeLumpSum"",""value"":111727.22},{""name"":""totalPension"",""value"":16336.03}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":12806.27},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3424.38}]},{""name"":""reducedPension_DCAsOMOAnnuity"",""attributes"":[{""name"":""annuityPurchaseAmount"",""value"":11308.22},{""name"":""totalLTAUsedPerc"",""value"":40.35},{""name"":""totalPension"",""value"":16336.03}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":12806.27},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3424.38}]},{""name"":""reducedPension_DCAsTransfer"",""attributes"":[{""name"":""transferValueOfDC"",""value"":0.0},{""name"":""totalLTAUsedPerc"",""value"":0.0},{""name"":""totalPension"",""value"":16336.03}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":12806.27},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3424.38}]},{""name"":""fullPensionDCAsLumpSum"",""attributes"":[{""name"":""totalSpousePension"",""value"":10825.44},{""name"":""totalPension"",""value"":21650.88},{""name"":""totalLTAUsedPerc"",""value"":40.35},{""name"":""totalLumpSum"",""value"":11308.22},{""name"":""lumpSumFromDB"",""value"":0.0},{""name"":""lumpSumFromDC"",""value"":11308.22},{""name"":""totalTaxFreeLumpSum"",""value"":11308.22}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":17700.34},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3845.16}]},{""name"":""reducedPensionDCAsLumpSum"",""attributes"":[{""name"":""totalSpousePension"",""value"":10825.44},{""name"":""totalPension"",""value"":16887.96},{""name"":""totalLumpSum"",""value"":108900.16},{""name"":""totalTaxFreeLumpSum"",""value"":108900.16},{""name"":""lumpSumFromDB"",""value"":97591.94},{""name"":""lumpSumFromDC"",""value"":11308.22},{""name"":""minimumPermittedTotalLumpSum"",""value"":11408.22},{""name"":""maximumPermittedTotalLumpSum"",""value"":111732.22},{""name"":""totalLTAUsedPerc"",""value"":41.64}],""pensionTranches"":[{""trancheTypeCode"":""adjustmentGMPEq"",""increaseTypeCode"":""NILGMPEQ"",""value"":105.38},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5GMPEARLY"",""value"":13241.75},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":3540.83}]}],""wordingFlags"":[""GMP"",""GMPEQCHECKED"",""GMPEARLY"",""GMPPOST88"",""GMPPRE88"",""SELECTEDDATEOVERNRA"",""GMPEQCHECKED"",""tranche_adjustmentGMPEq_NILGMPEQ"",""tranche_pre97Excess_LPI5GMPEARLY"",""tranche_post97_LPI5""]}";
    public const string SelectedQuoteV2ReducedPensionNestedLumsumSmallerThanMaxPermittedLumpSumDict = @"{ ""reducedPension_totalSpousePension"" : 10825.44 , ""reducedPension_totalPension"" : 16336.03 , ""reducedPension_totalLumpSum"" : 108900.16 , ""reducedPension_totalTaxFreeLumpSum"" : 108900.16 , ""reducedPension_lumpSumFromDB"" : 108900.16 , ""reducedPension_lumpSumFromDC"" : 0.0 , ""reducedPension_minimumPermittedTotalLumpSum"" : 100.0 , ""reducedPension_maximumPermittedTotalLumpSum"" : 108905.16 , ""reducedPension_totalLTAUsedPerc"" :  40.59 , ""totalDCFundValueForChoice"" : 11308.22 , ""reducedPension_pensionTranches_post97"" : 3424.38 }";
    public const string RetirementV2ReducedPensionLumsumSameAsMaxPermittedLumpSumJson = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1962-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""totalFundValue"":null,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""minimumPermittedTotalLumpSum"":0,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalLTAUsedPerc"",""value"":14.72},{""name"":""totalPension"",""value"":32369.4},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""reducedPension"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":165346.35},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""maximumPermittedTotalLumpSum"",""value"":165346.35},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""totalLTAUsedPerc"",""value"":16.03},{""name"":""totalLumpSum"",""value"":165346.35},{""name"":""totalPension"",""value"":24801.96},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88"",""GMPEQCHECKED"",""tranche_post88GMP_GMP"",""tranche_post97_LPI5"",""tranche_pre88GMP_NIL"",""tranche_pre97Excess_LPI5""]}";
    public const string SelectedQuoteV2ReducedPensionLumsumSameAsMaxPermittedLumpSumDict = @"{ ""reducedPension_totalSpousePension"" : 6387.12 , ""reducedPension_totalPension"" : 9975.12 , ""reducedPension_totalLumpSum"" : 66577.62 , ""reducedPension_totalTaxFreeLumpSum"" : 66577.62 , ""reducedPension_lumpSumFromDB"" : 58111.4 , ""reducedPension_lumpSumFromDC"" : 8466.22 , ""reducedPension_minimumPermittedTotalLumpSum"" : 8566.22 , ""reducedPension_maximumPermittedTotalLumpSum"" : 66577.62 , ""reducedPension_totalLTAUsedPerc"" :  24.81 , ""reducedPension_pensionTranches_post97"" : 9975.12 }";
    public const string RetirementV2DCJourneyLumpSumLessThanMaxPermittedLumpSum = @"{""calculationEventType"":""CE"",""dateOfBirth"":""0001-01-01T00:00:00"",""datePensionableServiceCommenced"":null,""dateOfLeaving"":null,""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":null,""post88GMPIncreaseCap"":null,""pre88GMPAtGMPAge"":null,""post88GMPAtGMPAge"":null,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0,""externalAvcFundValue"":0,""totalAvcFundValue"":0,""totalFundValue"":57697.04,""standardLifetimeAllowance"":0,""totalLtaUsedPercentage"":5.37,""minimumPermittedTotalLumpSum"":1.0,""maximumPermittedTotalLumpSum"":14424.26,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":null,""inputEffectiveDate"":""2025-01-25"",""quotesV2"":[{""name"":""annuityBrokerHUBTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":1}],""pensionTranches"":[]},{""name"":""annuityBrokerHUBFull"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":2}],""pensionTranches"":[]},{""name"":""annuityOMTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":3}],""pensionTranches"":[]},{""name"":""annuityOMFull"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":4}],""pensionTranches"":[]},{""name"":""incomeDrawdownTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":5}],""pensionTranches"":[]},{""name"":""incomeDrawdownITF"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":6}],""pensionTranches"":[]},{""name"":""incomeDrawdownOMTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":7}],""pensionTranches"":[]},{""name"":""incomeDrawdownOMITF"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":8}],""pensionTranches"":[]},{""name"":""cashLumpsum"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":9}],""pensionTranches"":[]},{""name"":""transferDC"",""attributes"":[{""name"":""totalTransferValue"",""value"":57697.04},{""name"":""optionNumber"",""value"":10}],""pensionTranches"":[]},{""name"":""transferDCToDB"",""attributes"":[{""name"":""totalTransferValue"",""value"":57697.04},{""name"":""optionNumber"",""value"":11}],""pensionTranches"":[]}],""wordingFlags"":[""TENANTIS2UFPLS""]}";
    public const string SelectedQuoteDetailsLessThanMaxDCJourney = @"{""totalFundValue"":57697.04,""annuityBrokerHUBTFC_taxableUFPLS"":46474.71,""annuityBrokerHUBTFC_taxFreeUFPLS"":11222.33,""annuityBrokerHUBTFC_optionNumber"":1,""totalLTAUsedPerc"":5.37,""selectedQuoteFullName"":""annuityBrokerHUBTFC""}";
    public const string RetirementV2DCJourneyLumpSumEqualToMaxPermittedLumpSum = @"{""calculationEventType"":""CE"",""dateOfBirth"":""0001-01-01T00:00:00"",""datePensionableServiceCommenced"":null,""dateOfLeaving"":null,""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":null,""post88GMPIncreaseCap"":null,""pre88GMPAtGMPAge"":null,""post88GMPAtGMPAge"":null,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0,""externalAvcFundValue"":0,""totalAvcFundValue"":0,""totalFundValue"":57697.04,""standardLifetimeAllowance"":0,""totalLtaUsedPercentage"":5.37,""minimumPermittedTotalLumpSum"":1.0,""maximumPermittedTotalLumpSum"":14424.26,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":null,""inputEffectiveDate"":""2025-01-25"",""quotesV2"":[{""name"":""annuityBrokerHUBTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":14424.26},{""name"":""optionNumber"",""value"":1}],""pensionTranches"":[]},{""name"":""annuityBrokerHUBFull"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":2}],""pensionTranches"":[]},{""name"":""annuityOMTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":3}],""pensionTranches"":[]},{""name"":""annuityOMFull"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":4}],""pensionTranches"":[]},{""name"":""incomeDrawdownTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":5}],""pensionTranches"":[]},{""name"":""incomeDrawdownITF"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":6}],""pensionTranches"":[]},{""name"":""incomeDrawdownOMTFC"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":7}],""pensionTranches"":[]},{""name"":""incomeDrawdownOMITF"",""attributes"":[{""name"":""taxableUFPLS"",""value"":57697.04},{""name"":""taxFreeUFPLS"",""value"":0.0},{""name"":""optionNumber"",""value"":8}],""pensionTranches"":[]},{""name"":""cashLumpsum"",""attributes"":[{""name"":""taxableUFPLS"",""value"":46474.71},{""name"":""taxFreeUFPLS"",""value"":11222.33},{""name"":""optionNumber"",""value"":9}],""pensionTranches"":[]},{""name"":""transferDC"",""attributes"":[{""name"":""totalTransferValue"",""value"":57697.04},{""name"":""optionNumber"",""value"":10}],""pensionTranches"":[]},{""name"":""transferDCToDB"",""attributes"":[{""name"":""totalTransferValue"",""value"":57697.04},{""name"":""optionNumber"",""value"":11}],""pensionTranches"":[]}],""wordingFlags"":[""TENANTIS2UFPLS""]}";
    public const string SelectedQuoteDetailsEqualToMaxDCJourney = @"{""totalFundValue"":57697.04,""annuityBrokerHUBTFC_taxableUFPLS"":46474.71,""annuityBrokerHUBTFC_taxFreeUFPLS"":14424.26,""annuityBrokerHUBTFC_optionNumber"":1,""totalLTAUsedPerc"":5.37,""selectedQuoteFullName"":""annuityBrokerHUBTFC""}";


    public const string RetirementJsonV2 = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1962-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""totalFundValue"":null,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""minimumPermittedTotalLumpSum"":0,""maximumPermittedTotalLumpSum"":165346.35,""maximumPermittedStandardLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""inputEffectiveDate"":""2024-10-24"",""residualFundValue"":null,""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalLTAUsedPerc"",""value"":14.72},{""name"":""totalPension"",""value"":32369.4},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""reducedPension"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":165346.35},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""maximumPermittedTotalLumpSum"",""value"":165346.35},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""totalLTAUsedPerc"",""value"":16.03},{""name"":""totalLumpSum"",""value"":165346.35},{""name"":""totalPension"",""value"":24801.96},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88"",""GMPEQCHECKED"",""tranche_post88GMP_GMP"",""tranche_post97_LPI5"",""tranche_pre88GMP_NIL"",""tranche_pre97Excess_LPI5""]}";
    public const string RetirementJsonV2New = @"{""calculationEventType"":""PV"",""dateOfBirth"":""1960-11-07T00:00:00"",""datePensionableServiceCommenced"":""1980-08-05T00:00:00"",""dateOfLeaving"":""2010-09-06T00:00:00"",""statePensionDate"":null,""statePensionDeduction"":null,""gmpAge"":""P60Y0M"",""post88GMPIncreaseCap"":3,""pre88GMPAtGMPAge"":960.96,""post88GMPAtGMPAge"":1604.2,""transferInService"":null,""totalPensionableService"":null,""finalPensionableSalary"":null,""internalAVCFundValue"":0.0,""externalAvcFundValue"":0.0,""totalAvcFundValue"":0.0,""standardLifetimeAllowance"":1073100.0,""totalLtaUsedPercentage"":60.32,""maximumPermittedTotalLumpSum"":165346.35,""totalLtaRemainingPercentage"":0,""normalMinimumPensionAge"":""P55Y0M"",""quotesV2"":[{""name"":""fullPension"",""attributes"":[{""name"":""totalLTAUsedPerc"",""value"":14.72},{""name"":""totalPension"",""value"":32369.4},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":15249.7},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":14555.06}]},{""name"":""reducedPension"",""attributes"":[{""name"":""lumpSumFromDB"",""value"":165346.35},{""name"":""lumpSumFromDC"",""value"":0.0},{""name"":""maximumPermittedTotalLumpSum"",""value"":165346.35},{""name"":""minimumPermittedTotalLumpSum"",""value"":100.0},{""name"":""totalLTAUsedPerc"",""value"":16.03},{""name"":""totalLumpSum"",""value"":165346.35},{""name"":""totalPension"",""value"":24801.96},{""name"":""totalSpousePension"",""value"":12247.2}],""pensionTranches"":[{""trancheTypeCode"":""post88GMP"",""increaseTypeCode"":""GMP"",""value"":1604.2},{""trancheTypeCode"":""post97"",""increaseTypeCode"":""LPI5"",""value"":11684.6},{""trancheTypeCode"":""pre88GMP"",""increaseTypeCode"":""NIL"",""value"":960.44},{""trancheTypeCode"":""pre97Excess"",""increaseTypeCode"":""LPI5"",""value"":10552.72}]}],""wordingFlags"":[""GMP"",""GMPINPAY"",""GMPPOST88"",""GMPPRE88"",""GMPEQCHECKED"",""tranche_post88GMP_GMP"",""tranche_post97_LPI5"",""tranche_pre88GMP_NIL"",""tranche_pre97Excess_LPI5""]}";
    public const string RetiremntDatesAgesJson = @"{""earliestRetirementAge"":55,""latestRetirementAge"":null,""normalRetirementAge"":60, ""ageAtNormalRetirementIso"":""P60Y17D"",""earliestRetirementDate"":""2017-11-07T00:00:00+00:00"",""latestRetirementDate"":null,""normalRetirementDate"":""2022-11-07T00:00:00+00:00"",""targetRetirementAgeIso"": ""P65Y20D"", ""targetRetirementAgeYearsIso"": ""P66Y"",""targetRetirementDate"":""2022-11-07T00:00:00+02:00"",""wordingFlags"":[]}";
    public const string RetiremntDatesAgesJson2 = @"{""earliestRetirementAge"":55,""latestRetirementAge"":null,""normalRetirementAge"":60, ""ageAtNormalRetirementIso"":""P60Y17D"",""earliestRetirementDate"":""2017-11-07T00:00:00+00:00"",""latestRetirementDate"":null,""normalRetirementDate"":""2022-11-07T00:00:00+00:00"",""targetRetirementAgeIso"": ""P66Y20D"", ""targetRetirementAgeYearsIso"": ""P66Y"",""targetRetirementDate"":""2022-12-07T00:00:00+02:00"",""wordingFlags"":[]}";
    public const string RetiremntDatesAgesJson3 = @"{""earliestRetirementAge"":55,""latestRetirementAge"":null,""normalRetirementAge"":60, ""ageAtNormalRetirementIso"":""P60Y17D"",""earliestRetirementDate"":""2017-11-07T00:00:00+00:00"",""latestRetirementDate"":null,""normalRetirementDate"":""2022-11-07T00:00:00+00:00"",""targetRetirementAgeIso"": ""P66Y20D"", ""targetRetirementAgeYearsIso"": ""P66Y"",""targetRetirementDate"":""2022-12-07T00:00:00+02:00"",""wordingFlags"":[ ""SRD_USED"" ]}";
    public const string RetirementDatesAgesJsonNew = @"{""earliestRetirementAge"":55,""latestRetirementAge"":null,""normalRetirementAge"":65,""earliestRetirementDate"":""2022-11-07T00:00:00+00:00"",""latestRetirementDate"":null,""normalRetirementDate"":""2023-11-07T00:00:00+00:00""}";
    public const string TransferQuoteJson = @"{""wordingFlags"":[""transferFlag1"",""transferFlag2""],""guaranteeDate"":""2022-11-12T00:00:00+02:00"",""guaranteePeriod"":""P3M"",""replyByDate"":""2022-10-22T00:00:00+03:00"",""totalPensionAtDOL"":19514.56,""maximumResidualPension"":17910.46,""minimumResidualPension"":0.1,""transferValues"":{""totalGuaranteedTransferValue"":1074446.46,""totalNonGuaranteedTransferValue"":146334.09,""minimumPartialTransferValue"":537223.23,""totalTransferValue"":1220780.55},""isGuaranteedQuote"":true,""originalEffectiveDate"": ""2022-11-12""}";
    public const string RetirementQuotesJsonV2 = @"{""options"":{""fullPension"":{""attributes"":{""pensionTranches"":{""post97"":21534.48,""total"":21534.48},""totalPension"":21534.48,""totalSpousePension"":10767.24},""options"":{""DCAsOMOAnnuity"":{""attributes"":{""annuityPurchaseAmount"":6614.39,""totalLTAUsedPerc"":40.75}},""DCAsPCLS"":{""attributes"":{""annuityPurchaseAmount"":4960.79,""lumpSumFromDB"":0.0,""lumpSumFromDC"":1653.6,""totalLTAUsedPerc"":40.75,""totalLumpSum"":1653.6}},""DCAsTransfer"":{""attributes"":{""totalLTAUsedPerc"":40.13,""transferValueOfDC"":6614.39}},""DCAsUFPLS"":{""attributes"":{""taxFreeUFPLS"":1653.6,""taxableUFPLS"":4960.79,""totalLTAUsedPerc"":40.75,""totalUFPLS"":6614.39}}}},""fullPensionDCAsLumpSum"":{""attributes"":{""lumpSumFromDB"":0.0,""lumpSumFromDC"":6614.39,""pensionTranches"":{""post97"":21534.48,""total"":21534.48},""totalLTAUsedPerc"":40.13,""totalLumpSum"":6614.39,""totalPension"":21534.48,""totalSpousePension"":10767.24}},""reducedPension"":{""attributes"":{""lumpSumFromDB"":105040.0,""lumpSumFromDC"":0.0,""maximumPermittedTotalLumpSum"":105040.0,""minimumPermittedTotalLumpSum"":100.0,""pensionTranches"":{""post97"":16500.12,""total"":16500.12},""totalLTAUsedPerc"":41.15,""totalLumpSum"":105040.0,""totalPension"":16500.12,""totalSpousePension"":10767.24},""options"":{""DCAsOMOAnnuity"":{""attributes"":{""annuityPurchaseAmount"":6614.39,""totalLTAUsedPerc"":40.13}},""DCAsPCLS"":{""attributes"":{""annuityPurchaseAmount"":4960.79,""lumpSumFromDB"":0.0,""lumpSumFromDC"":1653.6,""totalLTAUsedPerc"":41.15,""totalLumpSum"":1653.6}},""DCAsTransfer"":{""attributes"":{""totalLTAUsedPerc"":40.13,""transferValueOfDC"":6614.39}},""DCAsUFPLS"":{""attributes"":{""taxFreeUFPLS"":1653.6,""taxableUFPLS"":4960.79,""totalLTAUsedPerc"":41.15,""totalUFPLS"":6614.39}}}},""reducedPensionDCAsLumpSum"":{""attributes"":{""lumpSumFromDB"":105040.0,""lumpSumFromDC"":6614.39,""maximumPermittedTotalLumpSum"":111654.39,""minimumPermittedTotalLumpSum"":6714.39,""pensionTranches"":{""post97"":16727.16,""total"":16727.16},""totalLTAUsedPerc"":41.15,""totalLumpSum"":111654.39,""totalPension"":16727.16,""totalSpousePension"":10767.24}}},""totalAVCFundValue"":6614.39,""standardLifetimeAllowance"":1073100.0,""externalAVCFundValue"":0.0,""internalAVCFundValue"":6614.39,""totalLTARemainingPerc"":0,""totalLTAUsedPerc"":40.87,""maximumPermittedTotalLumpSum"":111654.39,""datePensionableServiceCommenced"":""2006-08-14T00:00:00"",""calculationFactorDate"":""2025-03-28T00:00:00"",""dateOfBirth"":""1970-02-13T00:00:00"",""dateOfLeaving"":null,""statePensionDate"":null,""transferInService"":null,""totalPensionableService"":""P23Y6M"",""finalPensionableSalary"":54981.65,""wordingFlags"":[],""calcSystemHistorySeqno"": 72,""GMPAge"":null,""pre88GMPAtGMPAge"":null,""post88GMPAtGMPAge"":null,""post88GMPIncreaseCap"":null,""statePensionDeduction"":null,""trancheIncreaseMethods"":{""post97"":""LPI2_5""},""statutoryFactors"":{""normalMinimumPensionAge"":""P55Y0M"",""standardLifetimeAllowance"":1073100.0}}";
    public const string RateOfReturnResponseJson = @"{""rawInputs"":{""effectiveDate"":""2024-11-25"",""startDate"":""2023-11-25""},""inputs"":{""effectiveDate"":""2024-11-25"",""engineWriteResults"":false,""engineCalcSource"":""ASR"",""startDate"":""2023-11-25""},""errors"":{""warnings"":[],""fatals"":[]},""results"":{""benefits"":{""_changeInValue"":7331.91,""_closingBalance"":51764.96,""_closingBalanceZeroFlag"":""N"",""_contsRecord"":{""Dates"":[],""Amounts"":[],""TransactionTypes"":[]},""_correctedStartDate"":""2023-11-25"",""_dailyIRR"":0.00041738191728390994,""_openingBalance"":44433.05,""_personalRateOfReturn"":16.501,""_subTotal"":0,""_totalContributions"":0,""_totalDeductions"":0},""mdp"":{""rateOfReturn"":{""personalRateOfReturn"":16.501,""openingBalance"":44433.05,""closingBalance"":51764.96,""totalContributions"":0,""totalDeductions"":0,""changeInValue"":7331.91,""endDate"":""2024-11-25"",""subTotal"":0,""correctedStartDate"":""2023-11-25"",""closingBalanceZero"":""N"",""currency"":""GBP""}}}}";
    public const string TransferQuoteResponseJson = @"{
    ""errors"": {
        ""fatals"": [],
        ""warnings"": []
    },
    ""inputs"": {
        ""effectiveDate"": ""2022-11-18""
    },
    ""results"": { 
        ""mdp"": {
            ""GUARANTEE_PERIOD"": ""P3M"",
            ""REPLY_PERIOD"": ""P21D"",
            ""externalAVCFundDates"": [
                ""2022-04-01""
            ],
            ""guaranteeDate"": ""2023-02-16"",
            ""guaranteePeriod"": ""P3M"",
            ""maximumResidualPension"": 369.5,
            ""minimumResidualPension"": 0.0,
            ""originalEffectiveDate"": ""2022-11-16"",
            ""replyByDate"": ""2023-01-26"",
            ""totalPensionAtDOL"": 739.0,
            ""transferValues"": {
                ""isGuaranteedQuote"": true,
                ""minimumPartialTransferValue"": 11094.81,
                ""totalGuaranteedTransferValue"": 22189.62,
                ""totalNonGuaranteedTransferValue"": 1059.46,
                ""totalTransferValue"": 23249.08
            }
        }
    }
}";
    public const string RetirementResponseJson = @"{
    ""errors"": {
        ""fatals"": [],
        ""warnings"": []
    },
    ""inputs"": {
        ""disableTransferCalc"": ""True"",
        ""effectiveDate"": ""2022-11-07""
    },
    ""results"": {
        ""mdp"": {
        ""GMPAge"": ""P60Y0M"",
            ""dateOfBirth"": ""1962-11-07"",
            ""dateOfLeaving"": ""2010-09-06"",
            ""datePensionableServiceCommenced"": ""1980-08-05"",
            ""externalAVCFundValue"": 0.0,
            ""fullPension"": {
            ""annuityPurchaseAmount"": 0.0,
                ""lumpSumFromDB"": 0.0,
                ""lumpSumFromDC"": 0.0,
                ""maximumPermittedTotalLumpSum"": 0.0,
                ""minimumPermittedTotalLumpSum"": 0.0,
                ""pensionTranches"": {
                ""post88GMP"": 1604.2,
                    ""post97"": 15249.7,
                    ""pre88GMP"": 960.44,
                    ""pre97Excess"": 14555.06,
                    ""total"": 32369.4
                },
                ""smallPotLumpSum"": 0.0,
                ""taxFreeUFPLS"": 0.0,
                ""taxableUFPLS"": 0.0,
                ""totalLTAUsedPerc"": 14.72,
                ""totalLumpSum"": 0.0,
                ""totalPension"": 32369.4,
                ""totalSpousePension"": 12247.2,
                ""totalUFPLS"": 0.0,
                ""transferValueOfDC"": 0.0,
                ""trivialCommutationLumpSum"": 0.0
            },
            ""internalAVCFundValue"": 0.0,
            ""maximumPermittedTotalLumpSum"": 165346.35,
            ""post88GMPAtGMPAge"": 1604.2,
            ""post88GMPIncreaseCap"": 3,
            ""pre88GMPAtGMPAge"": 960.96,
            ""reducedPension"": {
            ""annuityPurchaseAmount"": 0.0,
                ""lumpSumFromDB"": 165346.35,
                ""lumpSumFromDC"": 0.0,
                ""maximumPermittedTotalLumpSum"": 165346.35,
                ""minimumPermittedTotalLumpSum"": 100.0,
                ""pensionTranches"": {
                ""post88GMP"": 1604.2,
                    ""post97"": 11684.6,
                    ""pre88GMP"": 960.44,
                    ""pre97Excess"": 10552.72,
                    ""total"": 24801.96
                },
                ""smallPotLumpSum"": 0.0,
                ""taxFreeUFPLS"": 0.0,
                ""taxableUFPLS"": 0.0,
                ""totalLTAUsedPerc"": 16.03,
                ""totalLumpSum"": 165346.35,
                ""totalPension"": 24801.96,
                ""totalSpousePension"": 12247.2,
                ""totalUFPLS"": 0.0,
                ""transferValueOfDC"": 0.0,
                ""trivialCommutationLumpSum"": 0.0
            },
            ""standardLifetimeAllowance"": 1073100.0,
            ""statutoryFactors"": {
            ""normalMinimumPensionAge"": ""P55Y0M"",
                ""standardLifetimeAllowance"": 1073100.0
            },
            ""totalAVCFundValue"": 0.0,
            ""totalLTAUsedPerc"": 60.32,
            ""trancheIncreaseMethods"": {
            ""post88GMP"": ""GMP"",
                ""post97"": ""LPI5"",
                ""pre88GMP"": ""NIL"",
                ""pre97Excess"": ""LPI5""
            },
            ""wordingFlags"": [
                ""GMP"",
                ""GMPINPAY"",
                ""GMPPOST88"",
                ""GMPPRE88""
            ]
        }
    }
}
";
    public const string RetirementV2ResponseJson = @"{
    ""errors"": {
        ""fatals"": [],
        ""warnings"": []
    },
    ""inputs"": {
        ""disableTransferCalc"": ""True"",
        ""effectiveDate"": ""2022-11-11""
    },
    ""results"": {
      ""quotation"": {
        ""guaranteed"": true,
        ""expiryDate"": ""2025-06-11""
      },
        ""mdp"": {
            ""GMPAge"": ""P60Y0M"",
            ""dateOfBirth"": ""1962-11-07"",
            ""dateOfLeaving"": ""2010-09-06"",
            ""datePensionableServiceCommenced"": ""1980-08-05"",
            ""externalAVCFundValue"": 0.0,
            ""fullPension"": {
                ""annuityPurchaseAmount"": 0.0,
                ""lumpSumFromDB"": 0.0,
                ""lumpSumFromDC"": 0.0,
                ""maximumPermittedTotalLumpSum"": 0.0,
                ""minimumPermittedTotalLumpSum"": 0.0,
                ""pensionTranches"": {
                    ""post88GMP"": 1604.2,
                    ""post97"": 15249.7,
                    ""pre88GMP"": 960.44,
                    ""pre97Excess"": 14555.06,
                    ""total"": 32369.4
                },
                ""smallPotLumpSum"": 0.0,
                ""taxFreeUFPLS"": 0.0,
                ""taxableUFPLS"": 0.0,
                ""totalLTAUsedPerc"": 14.72,
                ""totalLumpSum"": 0.0,
                ""totalPension"": 32369.4,
                ""totalSpousePension"": 12247.2,
                ""totalUFPLS"": 0.0,
                ""transferValueOfDC"": 0.0,
                ""trivialCommutationLumpSum"": 0.0
            },
            ""internalAVCFundValue"": 0.0,
            ""maximumPermittedTotalLumpSum"": 165346.35,
            ""options"": {
                ""fullPension"": {
                    ""attributes"": {
                        ""pensionTranches"": {
                            ""post88GMP"": 1604.2,
                            ""post97"": 15249.7,
                            ""pre88GMP"": 960.44,
                            ""pre97Excess"": 14555.06,
                            ""total"": 32369.4
                        },
                        ""totalLTAUsedPerc"": 14.72,
                        ""totalPension"": 32369.4,
                        ""totalSpousePension"": 12247.2
                    },
                    ""options"": {
                        ""DCAsOMOAnnuity"": {
                            ""attributes"": {
                                ""annuityPurchaseAmount"": 8466.22,
                                ""totalLTAUsedPerc"": 21.17
                            }
                        }}
                },
                ""reducedPension"": {
                    ""attributes"": {
                        ""lumpSumFromDB"": 165346.35,
                        ""lumpSumFromDC"": 0.0,
                        ""maximumPermittedTotalLumpSum"": 165346.35,
                        ""minimumPermittedTotalLumpSum"": 100.0,
                        ""pensionTranches"": {
                            ""post88GMP"": 1604.2,
                            ""post97"": 11684.6,
                            ""pre88GMP"": 960.44,
                            ""pre97Excess"": 10552.72,
                            ""total"": 24801.96
                        },
                        ""totalLTAUsedPerc"": 16.03,
                        ""totalLumpSum"": 165346.35,
                        ""totalPension"": 24801.96,
                        ""totalSpousePension"": 12247.2
                    }
                }
            },
            ""post88GMPAtGMPAge"": 1604.2,
            ""post88GMPIncreaseCap"": 3,
            ""pre88GMPAtGMPAge"": 960.96,
            ""reducedPension"": {
                ""annuityPurchaseAmount"": 0.0,
                ""lumpSumFromDB"": 165346.35,
                ""lumpSumFromDC"": 0.0,
                ""maximumPermittedTotalLumpSum"": 165346.35,
                ""minimumPermittedTotalLumpSum"": 100.0,
                ""pensionTranches"": {
                    ""post88GMP"": 1604.2,
                    ""post97"": 11684.6,
                    ""pre88GMP"": 960.44,
                    ""pre97Excess"": 10552.72,
                    ""total"": 24801.96
                },
                ""smallPotLumpSum"": 0.0,
                ""taxFreeUFPLS"": 0.0,
                ""taxableUFPLS"": 0.0,
                ""totalLTAUsedPerc"": 16.03,
                ""totalLumpSum"": 165346.35,
                ""totalPension"": 24801.96,
                ""totalSpousePension"": 12247.2,
                ""totalUFPLS"": 0.0,
                ""transferValueOfDC"": 0.0,
                ""trivialCommutationLumpSum"": 0.0
            },
            ""standardLifetimeAllowance"": 1073100.0,
            ""statutoryFactors"": {
                ""normalMinimumPensionAge"": ""P55Y0M"",
                ""standardLifetimeAllowance"": 1073100.0
            },
            ""totalAVCFundValue"": 0.0,
            ""residualFundValue"": 23477.3,
            ""totalLTAUsedPerc"": 60.32,
            ""trancheIncreaseMethods"": {
                ""post88GMP"": ""GMP"",
                ""post97"": ""LPI5"",
                ""pre88GMP"": ""NIL"",
                ""pre97Excess"": ""LPI5""
            },
            ""wordingFlags"": [
                ""GMP"",
                ""GMPINPAY"",
                ""GMPPOST88"",
                ""GMPPRE88"",
                ""GMPEQCHECKED""
            ],
            ""calcSystemHistorySeqno"": 72
        }
      }

    }";
    public const string RetitementDateAgesResponse = @"{
    ""retirementAges"": {
        ""earliestRetirementAge"": 55,
        ""latestRetirementAge"": null,
        ""normalRetirementAge"": 60,
        ""target"": ""P65Y20D"",
        ""targetDerivedInteger"": ""P66Y""
    },
    ""retirementDates"": {
        ""earliestRetirementDate"": ""2017-11-07T00:00:00+00:00"",
        ""latestRetirementDate"": null,
        ""normalRetirementDate"": ""2022-11-07T00:00:00+00:00"",
        ""target"": ""2022-11-07T00:00:00+00:00""
    },
    ""wordingFlags"": []
    }";
    public const string RetitementDateAgesResponseV2 = @"{
    ""retirementAges"": {
        ""earliestRetirementAge"": 55,
        ""latestRetirementAge"": 75,
        ""normalRetirementAge"": 60,
        ""target"": ""P65Y20D"",
        ""targetDerivedInteger"": ""P66Y"",
        ""normal"": ""P60Y17D""
    },
    ""retirementDates"": {
        ""earliestRetirementDate"": ""2017-11-07T00:00:00+00:00"",
        ""latestRetirementDate"": ""2022-11-07T00:00:00+00:00"",
        ""normalRetirementDate"": ""2022-11-07T00:00:00+00:00"",
        ""target"": ""2022-11-07T00:00:00+00:00""
    },
    ""wordingFlags"": []
    }";

    public const string ShortSummaryBlocks = @"{
  ""elements"": {
    ""description"": {
      ""elementType"": ""reference""
    },
    ""key"": {
      ""elementType"": ""text"",
      ""value"": ""reducedPension.short""
    },
    ""retirementTypeSetup"": {
      ""elementType"": ""text""
    },
    ""summaryBlocks"": {
      ""values"": [
        {
          ""elements"": {
            ""bottomInformation"": {
              ""elementType"": ""reference""
            },
            ""header"": {
              ""elementType"": ""text"",
              ""value"": """"
            },
            ""highlightedBackground"": {
              ""elementType"": ""toggle"",
              ""value"": false
            },
            ""summaryItems"": {
              ""values"": [
                {
                  ""elements"": {
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text"",
                      ""value"": """"
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Text"",
                        ""selection"": ""Text""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Chosen retirement date""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""[[token:selected_retirement_date]] (age [[token:selected_retirement_age]])\n""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""description"": {
                      ""elementType"": ""text""
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Text"",
                        ""selection"": ""Text""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Your choice""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""A reduced pension""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text"",
                      ""value"": """"
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
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
                      ""value"": ""Pension income""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""reducedPension.totalPension""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""description"": {
                      ""elementType"": ""text""
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Currency"",
                        ""selection"": ""Currency""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Tax-free lump sum""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""reducedPension.totalLumpSum""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""description"": {
                      ""elementType"": ""text"",
                      ""value"": ""Spouse, civil partner income may be payable if you die""
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
                      ""value"": ""Death benefits""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""reducedPension.totalSpousePension""
                    }
                  },
                  ""type"": ""Summary item""
                }
              ]
            }
          },
          ""type"": ""Summary block""
        }
      ]
    }
  },
  ""type"": ""Option summary""
}";

    public const string SummaryBlocks = @"{
  ""elements"": {
    ""dataSourceUrl"": {
            ""elementType"": ""text"",
            ""value"": """"
    },
    ""description"": {
      ""value"": {
        ""elements"": {
          ""content"": {
            ""elementType"": ""formattedtext"",
            ""value"": ""<p>The amount of pension given up for tax-free cash in this option has been based on factors recommended by the [[label:scheme_or_plan]]’s actuary, including taking account of average life expectancy. The figure quoted here is the maximum tax-free cash you can take. You can take a smaller lump sum than that shown below, in which case your total reduced pension would be higher.  You should consider whether the figures quoted are appropriate to your specific circumstances and you may need to take financial advice.</p>\n\n<p><strong>Remember:</strong> You can only apply to retire up to six months ahead of your chosen retirement date.</p>\n""
          },
          ""contentBlockKey"": {
            ""elementType"": ""text"",
            ""value"": ""summary_text_reducedPensionDCAsLumpSum""
          },
          ""dataSourceUrl"": {
            ""elementType"": ""text""
          },
          ""header"": {
            ""elementType"": ""text""
          },
          ""headerLink"": {
            ""elementType"": ""text""
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
    },
    ""key"": {
      ""elementType"": ""text"",
      ""value"": ""reducedPension""
    },
    ""retirementTypeSetup"": {
      ""elementType"": ""text"",
      ""value"": ""NON-ANN""
    },
    ""summaryBlocks"": {
      ""values"": [
        {
          ""elements"": {
            ""bottomInformation"": {
              ""elementType"": ""reference""
            },
            ""header"": {
              ""elementType"": ""text"",
              ""value"": ""In detail""
            },
            ""hideButtonColumn"": {
              ""elementType"": ""toggle""
            },
            ""highlightedBackground"": {
              ""elementType"": ""toggle"",
              ""value"": false
            },
            ""summaryItems"": {
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
                      ""elementType"": ""text""
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Text"",
                        ""selection"": ""Text""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Retirement date""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text"",
                      ""value"": """"
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
                      ""value"": ""[[token:selected_retirement_date]]""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""boldValue"": {
                      ""elementType"": ""toggle""
                    },
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text""
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Text"",
                        ""selection"": ""Text""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Retirement age""
                    },
                    ""link"": {
                      ""elementType"": ""text""
                    },
                    ""linkText"": {
                      ""elementType"": ""text"",
                      ""value"": """"
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
                      ""value"": ""[[token:selected_retirement_age]]""
                    }
                  },
                  ""type"": ""Summary item""
                }
              ]
            }
          },
          ""type"": ""Summary block""
        },
        {
          ""elements"": {
            ""bottomInformation"": {
							""values"": [
								{
									""elements"": {
										""content"": {
											""elementType"": ""formattedtext"",
											""value"": ""<p>If given, permission to deal with the person above regarding this application was provided on [[data-date:systemDate]]</p>""
										},
										""contentBlockKey"": {
											""elementType"": ""text""
										},
										""dataSourceUrl"": {
											""elementType"": ""text"",
											""value"": ""https://apim.awstas.net/mdp-api/api/retirement/cms-token-information""
										},
										""errorContent"": {
											""elementType"": ""formattedtext""
										},
										""header"": {
											""elementType"": ""text""
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
							]
						},
            ""header"": {
              ""elementType"": ""text"",
              ""value"": ""Total retirement benefits""
            },
            ""hideButtonColumn"": {
              ""elementType"": ""toggle""
            },
            ""highlightedBackground"": {
              ""elementType"": ""toggle"",
              ""value"": true
            },
            ""summaryItems"": {
              ""values"": [
                {
                  ""elements"": {
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text""
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Text"",
                        ""selection"": ""Text""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Your choice""
                    },
                    ""link"": {
                      ""elementType"": ""text"",
                      ""value"": ""explore_options_2""
                    },
                    ""linkText"": {
                      ""elementType"": ""text"",
                      ""value"": ""Change""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""A reduced pension""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text"",
                      ""value"": ""[[label:marginal_text_below_income]]\n[[label:view_option_pension_income_SPD]]""
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""values"": [
                        {
                          ""elements"": {
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
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.pre88GMP""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post88GMP_*]]""
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
                              ""value"": ""Guaranteed Minimum Pension accrued after 5 April 1988""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post88GMP""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_pre01Nov1996_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up before 1 November 1996""
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
                              ""value"": ""reducedPension.pensionTranches.pre01Nov1996""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_adjustmentGMPEq_NILGMPEQ]]""
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
                              ""value"": ""GMP equalisation adjustment""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.adjustmentGMPEq""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_pre93_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up before 1 January 1993""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.pre93""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post93_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up after 31 December 1992 and before 6 April 1997""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post93""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post31Oct1996_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up after 31 October 1996""
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
                              ""value"": ""reducedPension.pensionTranches.post31Oct1996""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_pre97Excess_LPI5GMPEARLY]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up before 6 April 1997""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.pre97Excess""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""If you left the [[label:scheme_or_plan]] prior to 1 November 1974, [[label:tranche_pre97ExcessBefore1974_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up before 6 April 1997""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text"",
                              ""value"": """"
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
                              ""value"": ""reducedPension.pensionTranches.pre97ExcessBefore1974""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""If you left the [[label:scheme_or_plan]] between 1 November 1974 and 30 April 1990, [[label:tranche_pre97Excess1974To1990_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up before 6 April 1997""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text"",
                              ""value"": """"
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
                              ""value"": ""reducedPension.pensionTranches.pre97Excess1974To1990""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""If you left the [[label:scheme_or_plan]] after 30 April 1990, [[label:tranche_pre97ExcessAfter1990_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up before 6 April 1997""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text"",
                              ""value"": """"
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
                              ""value"": ""reducedPension.pensionTranches.pre97ExcessAfter1990""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_bancoReal5_*]]""
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
                              ""value"": ""Banco Real 5% pension""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.bancoReal5""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_bancoReal3_*]]""
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
                              ""value"": ""Banco Real 3% pension""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.bancoReal3""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_bancoRealLPI_*]]""
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
                              ""value"": ""Banco Real LPI pension""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.bancoRealLPI""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post97to05_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up between 6 April 1997 and 5 April 2005""
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
                              ""value"": ""reducedPension.pensionTranches.post97to05""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post97_LPI5]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up after 5 April 1997""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post97""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post97to02_*]]""
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
                              ""value"": ""Pension built up between 6 April 1997 and 30 September 2002""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post97to02""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post97to04_*]]""
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
                              ""value"": ""Pension built up between 6 April 1997 and 31 March 2004""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post97to04""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post05_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up after 5 April 2005""
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
                              ""value"": ""reducedPension.pensionTranches.post05""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post97to06_*]]""
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
                              ""value"": ""Pension built up between 6 April 1997 and 5 April 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post97to06""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post02to06_*]]""
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
                              ""value"": ""Pension built up between 1 October 2002 and 5 April 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post02to06""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post04to06_*]]""
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
                              ""value"": ""Pension built up between 1 April 2004 and 5 April 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post04to06""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post06to06_*]]""
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
                              ""value"": ""Pension built up between 6 April 2006 and 30 June 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post06to06""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post30June2006_*]]""
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
                              ""value"": ""Pension built up after 30 June 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post30June2006""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post01July2006_*]]""
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
                              ""value"": ""Pension built up after 1 July 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post01July2006""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_post97ToPA04_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] built up between 6 April 1997 and 5 April 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.post97ToPA04""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_postPA04_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension built up after 6 April 2006""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.postPA04""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_cashBalance_*]]""
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
                              ""value"": ""Cash Balance pension""
                            },
                            ""link"": {
                              ""elementType"": ""text""
                            },
                            ""linkText"": {
                              ""elementType"": ""text""
                            },
                            ""tooltip"": {
                              ""elementType"": ""reference""
                            },
                            ""value"": {
                              ""elementType"": ""text"",
                              ""value"": ""reducedPension.pensionTranches.cashBalance""
                            }
                          },
                          ""type"": ""Summary item""
                        },
                        {
                          ""elements"": {
                            ""callToAction"": {
                              ""elementType"": ""reference""
                            },
                            ""description"": {
                              ""elementType"": ""text"",
                              ""value"": ""[[label:tranche_cashAccumulation_*]]""
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
                              ""value"": ""[[label:scheme_or_plan]] pension from your cash accumulation fund""
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
                              ""value"": ""reducedPension.pensionTranches.cashAccumulation""
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
                      ""value"": ""reducedPension.totalPension""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text"",
                      ""value"": """"
                    },
                    ""divider"": {
                      ""elementType"": ""toggle"",
                      ""value"": false
                    },
                    ""explanationSummaryItems"": {
                      ""elementType"": ""reference""
                    },
                    ""format"": {
                      ""elementType"": ""optionselection"",
                      ""value"": {
                        ""label"": ""Currency"",
                        ""selection"": ""Currency""
                      }
                    },
                    ""header"": {
                      ""elementType"": ""text"",
                      ""value"": ""Tax-free lump sum""
                    },
                    ""link"": {
                      ""elementType"": ""text"",
                      ""value"": ""change_lumpsum_v2""
                    },
                    ""linkText"": {
                      ""elementType"": ""text"",
                      ""value"": ""Change""
                    },
                    ""tooltip"": {
                      ""elementType"": ""reference""
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""reducedPension.totalLumpSum""
                    }
                  },
                  ""type"": ""Summary item""
                },
                {
                  ""elements"": {
                    ""callToAction"": {
                      ""elementType"": ""reference""
                    },
                    ""description"": {
                      ""elementType"": ""text"",
                      ""value"": ""Spouse, civil partner income may be payable if you die\n[[modal:view_death_benefits]]""
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
                      ""value"": ""Death benefits""
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
                      ""value"": {
                        ""elements"": {
                          ""contentText"": {
                            ""elementType"": ""formattedtext"",
                            ""value"": ""<p>[[label:view_options_default]]</p>\n""
                          },
                          ""headerText"": {
                            ""elementType"": ""text"",
                            ""value"": """"
                          },
                          ""linkText"": {
                            ""elementType"": ""text"",
                            ""value"": ""Spouse, civil partner income""
                          },
                          ""tooltipKey"": {
                            ""elementType"": ""text"",
                            ""value"": ""view_option_spouse_pen_info""
                          }
                        },
                        ""type"": ""Tooltip""
                      }
                    },
                    ""value"": {
                      ""elementType"": ""text"",
                      ""value"": ""reducedPension.totalSpousePension""
                    }
                  },
                  ""type"": ""Summary item""
                }
              ]
            }
          },
          ""type"": ""Summary block""
        }
      ]
    }
  },
  ""type"": ""Option summary""
}";

    public const string ContentBlocks = @"[
  {
    ""elements"": {
      ""content"": {
        ""elementType"": ""formattedtext"",
        ""value"": ""<p>While you were an active member of the Plan, you were contracted-out of SERPS/the State Second Pension, and you paid a lower rate of National Insurance contributions. As a result of this, you have built up a GMP in the Plan which is broadly the same as the pension you would have built up in the State Second Pension.</p>\n\n<p>When you reach your GMP age of [[data-text:gmpAgeYears]], some or all of your pension built up before 6 April 1997 will be replaced by the GMP.</p>\n\n<p>Your GMP built up after 5 April 1988 will be [[data-currency:post88GMPAtGMPAge]] a year and will receive increases in line with price inflation up to a maximum of [[data-text:post88GMPIncreaseCap]]% a year from the Plan while in payment.</p>\n""
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

    public const string GuaranteedQuoteResponse = @"{
    ""pagination"": {
        ""pageNumber"": 1,
        ""pageSize"": 5,
        ""totalCount"": 0,
        ""totalRecords"": 5
    },
    ""quotations"": [
        {
            ""runDate"": ""2025-02-05T00:00:00"",
            ""effectiveDate"": ""2024-12-19T00:00:00"",
            ""event"": ""transfer"",
            ""calcType"": ""PO"",
            ""imageId"": 13945593,
            ""calcSource"": ""IPA"",
            ""apiResponse"": {
                ""test"": 1
            },
            ""status"": ""ACCEPTED"",
            ""expiryDate"": ""2025-05-05T00:00:00""
        }
    ]
    }";
}