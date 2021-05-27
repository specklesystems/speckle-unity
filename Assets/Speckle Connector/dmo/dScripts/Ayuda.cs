using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace Speckle.ConnectorUnity {
    public readonly struct SpeckleSimpleInfo {

        public SpeckleSimpleInfo( string name )
            {
                this.name = name;
            }

        public string name { get; }

    }

    public static class Ayuda {

        public static void SyncCall( SendOrPostCallback post ) => SynchronizationContext.Current.Post( post, null );

        public static void ExecuteContinuations( )
            {
                var context = SynchronizationContext.Current;
                var execMethod = context.GetType( ).GetMethod( "Exec", BindingFlags.NonPublic | BindingFlags.Instance );
                execMethod.Invoke( context, null );
            }

        public static List<SpeckleSimpleInfo> GetInfo( this IEnumerable<Stream> input )
            {
                return (
                    from i in input
                    where i != null
                    select new SpeckleSimpleInfo( i.name ) ).ToList( );
            }

        public static List<SpeckleSimpleInfo> GetInfo( this IEnumerable<Branch> input )
            {
                return (
                    from i in input
                    where i != null
                    select new SpeckleSimpleInfo( i.name ) ).ToList( );
            }
        
        public static List<SpeckleSimpleInfo> GetInfo( this IEnumerable<Commit> input )
            {
                return (
                    from i in input
                    where i != null
                    select new SpeckleSimpleInfo( i.id ) ).ToList( );
            }
        public static IEnumerable<string> GetNames( this IEnumerable<Account> accounts )
            {
                List<string> names = new List<string>( );
                if ( accounts != null ) {
                    names.AddRange(
                        from account in accounts
                        where account != null
                        select account.userInfo.name
                    );
                }
                return names;
            }

    }
}