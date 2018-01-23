/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using NUnit.Framework;

namespace Leap.Unity {

  public class UtilsTests {

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
                                      "Constant Var")] string result) {
      Assert.That(Utils.GenerateNiceName(source), Is.EqualTo(result));
    }
  }
}
