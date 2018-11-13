using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace FakeEF.EFInterception
{
    public class FakeEfQueryProvider<T> : IQueryProvider where T : class, new()
    {
        private readonly DbContext dbContext;
        private readonly StubDbSet<T> stubDbSet;

        public FakeEfQueryProvider(StubDbSet<T> stubDbSet, DbContext dbContext)
        {
            this.stubDbSet = stubDbSet;
            this.dbContext = dbContext;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return stubDbSet.CurrentData.AsQueryable().Provider.CreateQuery(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression is MethodCallExpression method && method.Method.Name == "SelectMany")
            {
                foreach (var arg in method.Arguments.Skip(1))
                {
                    var unaryExpression = arg as UnaryExpression;
                    var operand = unaryExpression?.Operand as LambdaExpression;
                    var member = operand?.Body as MemberExpression;

                    if (member?.Member != null)
                    {
                        stubDbSet.AddInclude(member.Member.Name);
                    }
                }
            }


            return stubDbSet.CurrentData.AsQueryable().Provider.CreateQuery<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return stubDbSet.CurrentData.AsQueryable().Provider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var method = expression as MethodCallExpression;
            if (method != null && method.Method.Name == "FirstOrDefault")
            {
                foreach (var arg in method.Arguments.Skip(1))
                {
                    var unaryExpression = arg as UnaryExpression;
                    var operand = unaryExpression?.Operand as LambdaExpression;
                    var member = operand?.Body as BinaryExpression;
                    if (member?.Left.NodeType == ExpressionType.Convert)
                    {
                        var unary = (UnaryExpression) member.Left;
                        var memberExpression = unary.Operand as MemberExpression;
                        var ex = memberExpression?.Expression as MemberExpression;
                        if (ex?.Member.Name != null)
                        {
                            stubDbSet.AddInclude(ex.Member.Name);
                        }
                        else if (memberExpression?.Member.Name != null)
                        {
                            stubDbSet.AddInclude(memberExpression.Member.Name);
                        }
                    }
                    else
                    {
                        var leftMemberExpression = member?.Left as MemberExpression;
                        var accessedMember = leftMemberExpression?.Expression as MemberExpression;

                        if (accessedMember != null)
                        {
                            stubDbSet.AddInclude(accessedMember.Member.Name);
                        }
                    }
                }
            }

            return stubDbSet.CurrentData.AsQueryable().Provider.Execute<TResult>(expression);
        }
    }
}