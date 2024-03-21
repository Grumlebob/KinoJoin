using System.Linq.Expressions;
using System.Reflection;

namespace Domain.Helpers;

public static class GettingAttributeHelper
{
    /// <summary>
    ///  Get the MaxLength attribute from a string property
    ///  <typeparam name="T">The type to select a property from</typeparam>
    /// <param name="propertyExpression">An expression specifying property to get the MaxLength attribute from</param>
    ///  <example>
    /// GetMaxLength<JoinEvent>(e => e.Title))
    /// </example>
    ///  <returns>The length defined in the MaxLength attribute, if available</returns>
    ///  </summary>
    public static int? GetMaxLength<T>(Expression<Func<T, string>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            // Get the property information from the expression
            var propertyInfo = memberExpression.Member as PropertyInfo;

            if (propertyInfo != null)
            {
                // Get the MaxLength attribute applied to the property
                var maxLengthAttribute =
                    propertyInfo.GetCustomAttribute(typeof(MaxLengthAttribute)) as MaxLengthAttribute;

                // Return the length defined in the MaxLength attribute, if available
                return maxLengthAttribute?.Length;
            }
        }

        // Return null if the property doesn't have a MaxLength attribute
        return null;
    }
}