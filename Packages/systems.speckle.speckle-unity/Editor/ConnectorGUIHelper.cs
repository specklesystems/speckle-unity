using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;

namespace Speckle.ConnectorUnity
{
  public static class ConnectorGUIHelper
  {

    private const char SEP = '|';
    private const string FILL = "empty";

    private static string FormatForDrop(this string inputA, string inputB = null)
    {
      return!inputB.Valid() ? inputA : inputA + SEP + inputB;
    }
    
    private static void ParseFromDrop(this string value, out string inputA, out string inputB)
    {
      inputA = "";
      inputB = "";

      if (!value.Valid())
        return;

      var items = value.Split(SEP);
      inputA = items.FirstOrDefault();
      inputB = items.LastOrDefault();
    }

    // public static string Format(this SpeckleConvertersSO item)
    // {
    //   return item.name.FormatForDrop();
    // }

    public static string Format(this ISpeckleKit item)
    {
      return item.Name.FormatForDrop(item.Author);
    }

    public static string Format(this Account item)
    {
      return item != null ? item.userInfo.email.FormatForDrop(item.serverInfo.name) : string.Empty;
    }

    public static string Format(this Stream item)
    {
      return item != null ? item.name.FormatForDrop(item.id) : string.Empty;
    }

    public static string Format(this Branch item)
    {
      return item != null ? item.name.FormatForDrop() : string.Empty;
    }

    public static string Format(this Commit item)
    {
      return item != null ? item.id.FormatForDrop(item.message) : string.Empty;
    }

    // public static IEnumerable<string> Format(this IEnumerable<SpeckleConvertersSO> items)
    // {
    //   return items != null ? items.Select(x => x.Format()).ToArray() : new[] { "empty" };
    // }

    public static IEnumerable<string> Format(this IEnumerable<ISpeckleKit> items)
    {
      return items != null ? items.Select(x => x.Format()).ToArray() : new[] { FILL };
    }

    public static IEnumerable<string> Format(this IEnumerable<Account> items)
    {
      return items != null ? items.Select(x => x.Format()).ToArray() : new[] { FILL };
    }

    public static IEnumerable<string> Format(this IEnumerable<Stream> items)
    {
      return items != null ? items.Select(x => x.Format()).ToArray() : new[] { FILL };
    }

    public static IEnumerable<string> Format(this IEnumerable<Branch> items)
    {
      return items != null ? items.Select(x => x.Format()).ToArray() : new[] { FILL };
    }

    public static IEnumerable<string> Format(this IEnumerable<Commit> items)
    {
      return items != null ? items.Select(x => x.Format()).ToArray() : new[] { FILL };
    }

    public static void ParseForAccount(this string value, out string email, out string server)
    {
      email = "";
      server = "";
      
      value.ParseFromDrop(out email, out server);
    }
    
    public static void ParseForStream(this string value, out string name, out string id)
    {
      name = "";
      id = "";
      
      value.ParseFromDrop(out name, out id);
    }
    
    public static void ParseForBranch(this string value, out string name)
    {
      name = "";
      
      value.ParseFromDrop(out name, out var empty);
    }
    
    public static void ParseForCommit(this string value, out string id, out string msg)
    {
      id = "";
      msg = "";
      
      value.ParseFromDrop(out id, out msg);
    }
    
 
  }
}