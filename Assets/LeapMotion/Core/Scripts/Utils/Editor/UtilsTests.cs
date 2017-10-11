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
                                      "GetTheSCUBANow")] string source,
                              [Values("Private Var",
                                      "Mult By 32",
                                      "The Key Code",
                                      "Camel Case Too",
                                      "Is 2 Equal To 5",
                                      "Get The SCUBA Now")] string result) {
      Assert.That(Utils.GenerateNiceName(source), Is.EqualTo(result));
    }
  }
}
