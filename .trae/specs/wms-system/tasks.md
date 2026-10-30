# WMS 系统 — 实施任务清单

> 对应 spec.md　|　2026-09-09
> 状态：pending / in\_progress / completed / blocked / cancelled

## 阶段一：基础设施（骨架）

### Task 1：创建解决方案骨架与项目引用

* **优先级**：high

* **内容**：新建 `KSKJ_WMS.sln` 及 6 个项目（WMS\_Common / WMS\_Model / WMS\_Helper / WMS\_IServices / WMS\_Services / KSKJ\_WMS），建立项目引用：Api→IServices+Services；Services→IServices+Helper+Model；Helper→Common+Model；IServices→Common；Model→Common。每个 .csproj 引用 EF Core 8.0 等必要包。

* **依赖**：无

* **测试要求（rule）**：`dotnet build KSKJ_WMS.sln` 0 错误。

* **Status**：pending

### Task 2：WMS\_Common 层

* **优先级**：high

* **内容**：新建 `WMS_Common`，含 `Enums/ErrorCode.cs`、`Enums/WriteRoRead.cs`、`Enums/WmsEnums.cs`（单据状态、库位状态等）、`Helper/ResponseMessage.cs`、`Helper/ResponseException.cs`、`Helper/PaginationList.cs`。命名空间 `WMS_Common.*`。

* **依赖**：Task 1

* **测试要求（rule）**：编译通过；ErrorCode 取值与 HTTP 状态码对齐。

* **Status**：pending

### Task 3：WMS\_Model 层（全部实体）

* **优先级**：high

* **内容**：在 `WMS_Model/WMSModel` 下新建全部实体：Warehouse/Zone/Location、MaterialCategory、Material、DataDictionary、InboundOrder/InboundOrderLine、OutboundOrder/OutboundOrderLine、Inventory、InventoryAdjust、StockTransfer、StocktakeOrder/StocktakeLine、StockTransaction（出入库流水）、WcsTaskLog（WCS 任务下发记录）、WmsLogModel（日志）。全部带表/列映射特性（snake\_case），主键自增，可空字段用 `string?`。

* **依赖**：Task 1

* **测试要求（rule）**：编译通过；无嵌套模型类；全部实体位于 WMS\_Model 层。

* **Status**：pending

### Task 4：WMS\_Helper 层

* **优先级**：high

* **内容**：`WmsDbContext`（注册全部 DbSet + OnModelCreating 映射）、`ConfigurationHelper`（读 appsettings DBType/连接串，按类型选连接串）、`IDbContextFactory`/`DbContextFactory`、`WmsDbContextDesignTimeFactory`、`LogHelper`（落库 + 区分 WMS 业务日志，不写 txt）。

* **依赖**：Task 2、Task 3

* **测试要求（rule）**：编译通过；DbContext 含全部 DbSet；设计时工厂可被 EF 工具发现。

* **Status**：pending

### Task 5：IServices/Services 基类

* **优先级**：high

* **内容**：`IBaseService`（泛型 CRUD + 分页）、`BaseService`（经工厂创建上下文，动态排序）；`IServiceCollection` 扩展方法 `AddWmsServices` 自动注册 IService↔Service。

* **依赖**：Task 4

* **测试要求（rule）**：编译通过；基类提供 Query/Add/Update/Delete/分页方法。

* **Status**：pending

### Task 6：KSKJ\_WMS Api 宿主

* **优先级**：high

* **内容**：`Program.cs`（配置、Swagger、CORS、认证、`AddWmsServices`、GlobalExceptionFilter）、`Filters/GlobalExceptionFilter.cs`、`Extensions/WmsServiceCollectionExtensions.cs`、appsettings.json（SqlServer 指向 KSKJ\_WMS）。

* **依赖**：Task 5

* **测试要求（rule）**：编译通过；Swagger 可访问；全局异常返回 ResponseMessage。

* **Status**：pending

### Task 7：EF 迁移生成与建库

* **优先级**：high

* **内容**：`dotnet ef migrations add InitialCreate`（SQL Server）、新建 `KSKJ_WMS` 库、`database update` 建表。

* **依赖**：Task 6

* **测试要求（rule）**：KSKJ\_WMS 库中创建全部业务表 + `__EFMigrationsHistory`。

* **Status**：pending

## 阶段二：基础数据模块

### Task 8：库位管理

* **优先级**：high

* **内容**：`ILocationService`/`LocationService`/`LocationController`，库位 CRUD + 状态管理（空/有货/锁定/禁用），分页查询。

* **依赖**：Task 7

* **测试要求（rule）**：Swagger 可见 CRUD 接口；创建/查询/更新/删除库位正常。

* **Status**：pending

### Task 9：物料分类管理

* **优先级**：medium

* **内容**：分类 CRUD。

* **依赖**：Task 7

* **测试要求（rule）**：Swagger 可见 CRUD。

* **Status**：pending

### Task 10：物料/SKU 管理

* **优先级**：high

* **内容**：物料 CRUD（含是否启用批次/效期标志）。

* **依赖**：Task 9

* **测试要求（rule）**：Swagger 可见 CRUD。

* **Status**：pending

### Task 11：数据字典管理

* **优先级**：medium

* **内容**：字典 CRUD（按类型分组）。

* **依赖**：Task 7

* **测试要求（rule）**：Swagger 可见 CRUD。

* **Status**：pending

## 阶段三：入库管理

### Task 12：入库单 CRUD + 状态流转

* **优先级**：high

* **内容**：入库单（含明细行）CRUD，状态机：新建→已收货→上架中→已完成/已取消。新建后状态=新建。

* **依赖**：Task 10

* **测试要求（rule）**：可创建含多行明细的入库单；状态字段合法流转。

* **Status**：pending

### Task 13：收货确认

* **优先级**：high

* **内容**：收货接口登记实收数量，状态→已收货。

* **依赖**：Task 12

* **测试要求（rule）**：收货后状态为已收货，实收数量持久化。

* **Status**：pending

### Task 14：上架（库存增加 + WCS 任务下发）

* **优先级**：high

* **内容**：上架接口增加库存（按库位+物料+批次），状态→已完成；若配置 WcsBaseUrl 则生成 WCS 任务下发（失败入 retry\_messages 重发）。

* **依赖**：Task 13

* **测试要求（rule）**：上架后 inventory 表新增/增加对应库存；入库单状态=已完成。

* **Status**：pending

## 阶段四：出库管理

### Task 15：出库单 CRUD + 状态流转

* **优先级**：high

* **内容**：出库单（含明细行）CRUD，状态机：新建→已分配→拣货中→已发运/已取消。

* **依赖**：Task 10

* **测试要求（rule）**：可创建含多行明细的出库单。

* **Status**：pending

### Task 16：分配/拣货（FIFO 扣减库存 + WCS 下发）

* **优先级**：high

* **内容**：分配接口按 FIFO（或指定批次）扣减库存，生成拣货任务，状态→已分配→拣货中；WCS 任务下发（失败入重发）。

* **依赖**：Task 15

* **测试要求（rule）**：分配后库存按数量扣减；不足时抛 ResponseException。

* **Status**：pending

### Task 17：复核发运

* **优先级**：medium

* **内容**：发运接口确认出库数量，状态→已发运。

* **依赖**：Task 16

* **测试要求（rule）**：发运后状态=已发运。

* **Status**：pending

## 阶段五：库存管理

### Task 18：库存查询

* **优先级**：high

* **内容**：多条件（物料/库位/批次）分页查询库存。

* **依赖**：Task 14

* **测试要求（rule）**：按物料编码查询返回正确库存。

* **Status**：pending

### Task 19：库存调整

* **优先级**：medium

* **内容**：增/减库存并记录调整原因，写库存流水。

* **依赖**：Task 18

* **测试要求（rule）**：调整后库存数量正确，有流水记录。

* **Status**：pending

### Task 20：移库

* **优先级**：medium

* **内容**：库位 A→B 库存转移，可选生成 WCS 任务。

* **依赖**：Task 18

* **测试要求（rule）**：移库后 A 减 B 增，数量一致。

* **Status**：pending

### Task 21：盘点

* **优先级**：medium

* **内容**：盘点单 + 录入实盘数 + 盘盈盘亏自动调整库存。

* **依赖**：Task 19

* **测试要求（rule）**：盘盈盘亏后库存更新。

* **Status**：pending

## 阶段六：报表与 WCS 对接

### Task 22：报表查询

* **优先级**：low

* **内容**：库存余额报表、出入库流水报表（带时间范围过滤）。

* **依赖**：Task 18

* **测试要求（rule）**：报表接口返回数据。

* **Status**：pending

### Task 23：WCS 回调接口 + 任务下发重发

* **优先级**：high

* **内容**：`/api/wcs/callback` 接收 WCS 任务状态（完成/失败）并更新对应入库/出库单状态；`RetryRecorder`/`RetryService`/`RetryBackgroundService` 复用到 WMS（重发 WCS 任务下发）。

* **依赖**：Task 14、Task 16

* **测试要求（rule）**：调用回调接口后单据状态更新；WCS 下发失败入 retry\_messages。

* **Status**：pending

## 阶段八：前端工程（Vue3 + Element Plus）

### Task 25：前端工程脚手架

* **优先级**：high

* **内容**：在 `KSKJ_WMS_Web` 创建 Vue3 + Vite 工程，安装 Element Plus、Pinia、Vue Router、Axios；配置 vite proxy 转发后端；封装 Axios 请求（统一 baseURL、Cookie 凭证、ResponseMessage 解析、错误提示）；路由守卫（未登录跳登录）；登录页 + 主布局（左侧菜单+顶部+内容区）。

* **依赖**：Task 7（后端可启动）

* **测试要求（rule）**：`npm install && npm run build` 通过；登录页可访问；路由守卫生效。

* **Status**：pending

### Task 26：基础数据页面

* **优先级**：high

* **内容**：库位管理、物料分类、物料管理、数据字典四个页面（列表+新增/编辑弹窗+删除+分页+查询）。

* **依赖**：Task 8-11 后端接口

* **测试要求（rule）**：四个页面可增删改查，数据与后端联动。

* **Status**：pending

### Task 27：入库页面

* **优先级**：high

* **内容**：入库单列表、新建入库单（含明细行编辑）、收货、上架操作。

* **依赖**：Task 12-14

* **测试要求（rule）**：可创建入库单并走收货→上架流程，状态与库存同步。

* **Status**：pending

### Task 28：出库页面

* **优先级**：high

* **内容**：出库单列表、新建出库单、分配/拣货、发运操作。

* **依赖**：Task 15-17

* **测试要求（rule）**：可创建出库单并走分配→发运流程，库存扣减。

* **Status**：pending

### Task 29：库存页面

* **优先级**：high

* **内容**：库存查询、库存调整、移库、盘点页面。

* **依赖**：Task 18-21

* **测试要求（rule）**：库存查询可过滤；调整/移库/盘点操作生效。

* **Status**：pending

### Task 30：报表页面

* **优先级**：low

* **内容**：库存余额报表、出入库流水报表（时间范围过滤）。

* **依赖**：Task 22

* **测试要求（rule）**：报表数据展示正常。

* **Status**：pending

## 阶段九：验证

### Task 31：全量编译 + 迁移应用 + 前后端冒烟

* **优先级**：high

* **内容**：`dotnet build` 0 错误；迁移应用成功；后端启动 + 前端启动，Swagger 与前端页面均可访问，核心闭环（入库上架增库存→出库发运扣库存）端到端跑通。

* **依赖**：全部

* **测试要求（rule）**：R1-R12 全部通过；U1-U3 阈值达标。

* **Status**：pending

