using System.Reflection;
using WCS_IServices;

namespace WCS_Api.Extensions
{
    /// <summary>
    /// nan_T 2026-09-09：服务自动注册扩展（参考 DTcms.Core 使用 Autofac 按程序集约定注册的写法，
    /// 这里用 .NET 内置 DI 容器 + 反射实现，无需额外引入 Autofac 包）。
    /// 约定：WCS_Services 程序集中所有以 “Service” 结尾的非抽象类，
    /// 若实现了 WCS_IServices 程序集中定义的接口，则自动按“接口→实现”以 Scoped 生命周期注册，
    /// 新增服务时无需再到 Program.cs 手动 AddScoped。
    /// 注意：构造函数含 string/int 等原始类型参数的类（如 PLCService(string PLCIP)）
    /// 无法由容器自动构造，会被自动跳过，仍由代码手动 new 启动。
    /// </summary>
    public static class WcsServiceCollectionExtensions
    {
        /// <summary>
        /// 按约定自动注册 WCS_Services 程序集中的所有业务服务。
        /// </summary>
        /// <param name="services">DI 服务集合</param>
        /// <returns>服务集合（支持链式调用）</returns>
        public static IServiceCollection AddWcsServices(this IServiceCollection services)
        {
            // nan_T 2026-09-09：接口与实现所在程序集（接口程序集用于过滤“只注册本项目约定的服务接口”）
            Assembly serviceAssembly = Assembly.Load("WCS_Services");
            Assembly iserviceAssembly = Assembly.Load("WCS_IServices");

            // nan_T 2026-09-09：筛选约定类：类名以 Service 结尾、非抽象、可实例化
            var serviceTypes = serviceAssembly.GetTypes()
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase));

            foreach (var serviceType in serviceTypes)
            {
                // nan_T 2026-09-09：构造函数中含 string/值类型 等原始参数的类（如 PLCService），
                // 容器无法自动注入依赖，跳过自动注册，交由代码手动创建
                if (HasPrimitiveConstructorParameters(serviceType))
                {
                    continue;
                }

                // nan_T 2026-09-09：取该类实现的、且定义在 WCS_IServices 程序集中的接口
                var serviceInterfaces = serviceType.GetInterfaces()
                    .Where(i => i.Assembly == iserviceAssembly)
                    .ToList();

                if (serviceInterfaces.Count == 0)
                {
                    // nan_T 2026-09-09：没有实现约定接口的类（如纯工具/集合类）不注册
                    continue;
                }

                // nan_T 2026-09-09：注册为 Scoped（每个 HTTP 请求一个实例），与 DbContext 用法匹配
                foreach (var serviceInterface in serviceInterfaces)
                {
                    services.AddScoped(serviceInterface, serviceType);
                }
            }

            return services;
        }

        /// <summary>
        /// nan_T 2026-09-09：判断类型的构造函数是否包含容器无法自动注入的原始类型参数
        /// （string、int、bool、枚举等），包含则返回 true。
        /// </summary>
        private static bool HasPrimitiveConstructorParameters(Type type)
        {
            // nan_T 2026-09-09：取参数最多的公共构造函数作为 DI 选用构造函数来判断
            var constructor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                return false;
            }

            return constructor.GetParameters().Any(p =>
                p.ParameterType.IsPrimitive
                || p.ParameterType == typeof(string)
                || p.ParameterType.IsEnum
                || p.ParameterType == typeof(Guid));
        }
    }
}
