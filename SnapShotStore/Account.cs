using Akka;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnapShotStore
{
    [Serializable]
    public class Account
    {
        public Account(string accountID)
        {
            AccountID = accountID;
        }

        public string AccountID { get; private set; }
        public string CompanyIDCustomerID { get; set; }
        public string AccountTypeID { get; set; }
        public string PrimaryAccountCodeID { get; set; }
        public int PortfolioID { get; set; }
        public string ContractDate { get; set; }
        public string DelinquencyHistory { get; set; }
        public string LastPaymentAmount { get; set; }
        public string LastPaymentDate { get; set; }
        public string SetupDate { get; set; }
        public string CouponNumber { get; set; }
        public string AlternateAccountNumber { get; set; }
        public string Desc1 { get; set; }
        public string Desc2 { get; set; }
        public string Desc3 { get; set; }
        public string ConversionAccountID { get; set; }
        public string SecurityQuestionsAnswered { get; set; }
        public string LegalName { get; set; }
        public string RandomText0 { get; set; }
        public string RandomText1 { get; set; }
        public string RandomText2 { get; set; }
        public string RandomText3 { get; set; }
        public string RandomText4 { get; set; }
        public string RandomText5 { get; set; }
        public string RandomText6 { get; set; }
        public string RandomText7 { get; set; }
        public string RandomText8 { get; set; }
        public string RandomText9 { get; set; }
        /*
        protected Account(SerializationInfo info, StreamingContext context)
        {
            AccountID = info.GetString("AccountID");
            CompanyIDCustomerID = info.GetString("CompanyIDCustomerID");
            AccountTypeID = info.GetString("AccountTypeID");
            PrimaryAccountCodeID = info.GetString("PrimaryAccountCodeID");
            PortfolioID = info.GetInt32("PortfolioID");
            ContractDate = info.GetString("ContractDate");
            DelinquencyHistory = info.GetString("DelinquencyHistory");
            LastPaymentAmount = info.GetString("LastPaymentAmount");
            LastPaymentDate = info.GetString("LastPaymentDate");
            SetupDate = info.GetString("SetupDate");
            CouponNumber = info.GetString("CouponNumber");
            AlternateAccountNumber = info.GetString("AlternateAccountNumber");
            Desc1 = info.GetString("Desc1");
            Desc2 = info.GetString("Desc2");
            Desc3 = info.GetString("Desc3");
            ConversionAccountID = info.GetString("ConversionAccountID");
            SecurityQuestionsAnswered = info.GetString("SecurityQuestionsAnswered");
            LegalName = info.GetString("LegalName");
            RandomText0 = info.GetString("RandomText0");
            RandomText1 = info.GetString("RandomText1");
            RandomText2 = info.GetString("RandomText2");
            RandomText3 = info.GetString("RandomText3");
            RandomText4 = info.GetString("RandomText4");
            RandomText5 = info.GetString("RandomText5");
            RandomText6 = info.GetString("RandomText6");
            RandomText7 = info.GetString("RandomText7");
            RandomText8 = info.GetString("RandomText8");
            RandomText9 = info.GetString("RandomText9");
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AccountID", AccountID);
            info.AddValue("CompanyIDCustomerID", CompanyIDCustomerID);
            info.AddValue("AccountTypeID", AccountTypeID);
            info.AddValue("PrimaryAccountCodeID", PrimaryAccountCodeID);
            info.AddValue("PortfolioID", PortfolioID);
            info.AddValue("ContractDate", ContractDate);
            info.AddValue("DelinquencyHistory", DelinquencyHistory);
            info.AddValue("LastPaymentAmount", LastPaymentAmount);
            info.AddValue("LastPaymentDate", LastPaymentDate);
            info.AddValue("SetupDate", SetupDate);
            info.AddValue("CouponNumber", CouponNumber);
            info.AddValue("AlternateAccountNumber", AlternateAccountNumber);
            info.AddValue("Desc1", Desc1);
            info.AddValue("Desc2", Desc2);
            info.AddValue("Desc3", Desc3);
            info.AddValue("ConversionAccountID", ConversionAccountID);
            info.AddValue("SecurityQuestionsAnswered", SecurityQuestionsAnswered);
            info.AddValue("LegalName", LegalName);
            info.AddValue("RandomText0", RandomText0);
            info.AddValue("RandomText1", RandomText1);
            info.AddValue("RandomText2", RandomText2);
            info.AddValue("RandomText3", RandomText3);
            info.AddValue("RandomText4", RandomText4);
            info.AddValue("RandomText5", RandomText5);
            info.AddValue("RandomText6", RandomText6);
            info.AddValue("RandomText7", RandomText7);
            info.AddValue("RandomText8", RandomText8);
            info.AddValue("RandomText9", RandomText9);
        }
        */
    }





    /*

    "AccountID" string(15)  NOT NULL ,
    "CompanyIDCustomerID" string(26)  NOT NULL ,
    "AccountTypeID" string(6)  NOT NULL ,
    "PrimaryAccountCodeID" string(6)  NOT NULL ,
    "PortfolioID" int  NOT NULL ,
    "ContractDate" date  NOT NULL ,
    "DelinquencyHistory" string(max)  NOT NULL ,
    "LastPaymentAmount" money  NOT NULL ,
    "LastPaymentDate" date  NOT NULL ,
    "SetupDate" date  NOT NULL ,
    "CouponNumber" int  NOT NULL ,
    "AlternateAccountNumber" string(20)  NOT NULL ,
    "Desc1" string(6)  NOT NULL ,
    "Desc2" string(6)  NOT NULL ,
    "Desc3" string(6)  NOT NULL ,
    "ConversionAccountID" string(30)  NOT NULL ,
    "SecurityQuestionsAnswered" bit  NOT NULL ,
    "LegalName" string(1000)  NOT NULL ,
     */
}
