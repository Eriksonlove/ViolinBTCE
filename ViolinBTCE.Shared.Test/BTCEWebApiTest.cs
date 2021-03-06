﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using ViolinBtce.Dto;

namespace ViolinBtce.Shared.Test
{
    [TestFixture]
// ReSharper disable once InconsistentNaming
    public class BTCEWebApiTest
    {
        readonly BTCEWebApi _btceWebApi = new BTCEWebApi("key","secret");

        [TestCase(true, true)]
        [TestCase(true, false, ExpectedException = typeof(OperationCanceledException))]
        [TestCase(false, false, ExpectedException = typeof(OperationCanceledException))]
        public void Query(bool urlIsValid, bool urlExists)
        {
            // Pre-requirements
            if(urlExists)
                Assert.IsTrue(urlIsValid);

            // Arrange
            string urlString;
            if (urlIsValid)
                urlString = urlExists ? "http://www.google.com" : "http://dsadasd";
            else
                urlString = "http:dasasre2";

            // Act
            var queryResult = _btceWebApi.Query(urlString);
            
            // Assert
            if(urlIsValid)
                Assert.IsNotNull(queryResult);

        }


        [TestCase("key", "secret", "https://btc-e.com/tapi")]
        // This case only passes if your internet provider does not redirect not found pages to their own "error 404" page
        //[TestCase("key", "secret", "http://www.invalid.test",   ExpectedException = typeof(WebException))]

        [TestCase(null,  null,      "https://btc-e.com/tapi",   ExpectedException = typeof(ArgumentNullException))]
        [TestCase("",    "",        "https://btc-e.com/tapi",   ExpectedException = typeof(ArgumentNullException))]
        [TestCase("key", null,      "https://btc-e.com/tapi",   ExpectedException = typeof(ArgumentNullException))]
        [TestCase(null,  "secret",  "https://btc-e.com/tapi",   ExpectedException = typeof(ArgumentNullException))]

        [TestCase("key", "secret",  null,                       ExpectedException = typeof(OperationCanceledException))]
        [TestCase("key", "secret",  "",                         ExpectedException = typeof(OperationCanceledException))]
        [TestCase("key", "secret",  "invalidUriName",           ExpectedException = typeof(UriFormatException))]
        public void GetAnswerAsJsonString(string key, string secret, string apiUri)
        {
            // Arrange
            var operations = new Dictionary<string, string>
            {
                {"method", "dummyOperation"}
            };
            
            // Act
            var webApi = new BTCEWebApi(key, secret);
            string jsonString = webApi.GetAnswerAsJsonString(operations, apiUri);

            // Assert
            if (apiUri == "http://www.invalid.test")
                Assert.IsFalse(jsonString.Contains("success"));
            else
                Assert.IsTrue(jsonString.Contains("success"));

            // This must be ensured because each operation can only use one nonce a nonce can only be generated every one second 
            // due to its UnixTime nature.
            Thread.Sleep(1000);
        }

        [TestCase(true)]
        [TestCase(false,ExpectedException = typeof(OperationCanceledException) )]
        public void Deserialize(bool successExpected)
        {
            // Arrange
            DtoFunds dummyDtoFunds = new DtoFunds
            {
                Btc = 1,
                Ltc = 2
            };
            
            string returnString = "\"return\": " + JsonConvert.SerializeObject(dummyDtoFunds);
            const string errorMessage = "\"error\": \"Error message\"";

            int successValue = successExpected ? 1 : 0;
            string message   = successExpected ?  returnString : errorMessage;
            
            string jsonString = "{ \"success\": " + successValue + ", " + message + " }";

            // Act
            DtoFunds resultDto = _btceWebApi.Deserialize<DtoFunds>(jsonString);

            // Assert
            Assert.AreEqual(dummyDtoFunds,resultDto);
        }

        [Test]
        public void Deserialize_SpecificNameAssigned()
        {
            // Arrange
            const string jsonString = "{\"trade\":0.2}";

            // Act
            decimal result = _btceWebApi.Deserialize<decimal>(jsonString,"trade");

            // Assert
            Assert.AreEqual(0.2m , result);
        }


        [Test]
        public void Deserialize_OrderList()
        {
            // Arrange
            const string jsonString = "{\"success\":1,\"return\":{\"361683776\":{\"pair\":\"ltc_eur\",\"type\":\"sell\",\"amount\":0.10000000,\"rate\":100.00000000,\"timestamp_created\":1410085980,\"status\":0}}}";

            // Act
            var list = _btceWebApi.Deserialize<Dictionary<int, DtoOrder>>(jsonString);
            DtoActiveOrders resultDto = new DtoActiveOrders{ List = list };

            // Assert
            Assert.AreEqual(1, resultDto.List.Count);
            Assert.AreEqual(361683776,resultDto.List.First().Key);
        }
    }
}
