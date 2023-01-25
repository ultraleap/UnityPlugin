/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using NUnit.Framework;

namespace Leap.Unity.Tests
{
    public class UtilsTests
    {

        [Test]
        [Sequential]
        public void TestNiceNames([Values("_privateVar",
                                      "multBy32",
                                      "the_key_code",
                                      "CamelCaseToo",
                                      "_is2_equalTo_5",
                                      "GetTheSCUBANow",
                                      "m_privateVar",
                                      "kConstantVar")] string source,
                                  [Values("Private Var",
                                      "Mult By 32",
                                      "The Key Code",
                                      "Camel Case Too",
                                      "Is 2 Equal To 5",
                                      "Get The SCUBA Now",
                                      "Private Var",
                                      "Constant Var")] string result)
        {
            Assert.That(Utils.GenerateNiceName(source), Is.EqualTo(result));
        }
    }
}