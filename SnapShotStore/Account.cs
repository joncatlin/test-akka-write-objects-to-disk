using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnapShotStore
{
    //[Serializable]
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
