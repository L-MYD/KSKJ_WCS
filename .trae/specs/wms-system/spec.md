# WMS 仓储管理系统 — 需求规格

> 生成日期：2026-09-09　|　与 KSKJ_WCS 同一工作区，独立解决方案 KSKJ_WMS.sln

## 1. 背景与目标

当前工作区已有 KSKJ_WCS（仓储控制系统）。用户需要一个**独立的 WMS 仓储管理系统**，与 WCS 联动完成"业务单据 → 任务下发 → 设备执行 → 状态回传 → 库存更新"的完整闭环。

**目标**：交付一套可运行、结构清晰、可与 WCS 对接的 WMS 后端（Web API），覆盖仓储核心业务。

**非目标**：
- 不做复杂权限体系（沿用 KSKJ_WCS 的 Cookie 认证最简实现，仅区分登录与否，前端做路由守卫）。
- 不做 WMS 内部的复杂审批流。
- 不实现真实硬件/设备通讯（设备执行由 WCS 负责，WMS 只调 WCS 接口）。
- 不做移动端/PDA 界面（仅 PC 管理后台）。

## 2. 用户与场景

- **仓库管理员**：维护库位、物料、批次等基础数据；处理入库/出库单。
- **系统接口调用方（WCS / 上位 ERP）**：通过 WMS 开放的 HTTP 接口下发任务、回传状态、查询库存。

## 3. 功能范围

### 3.1 基础数据模块
- 仓库、库区、库位管理（CRUD + 库位状态：空/有货/锁定/禁用）
- 物料分类管理
- 物料/SKU 管理（编码、名称、规格、单位、批次/效期是否启用等）
- 数据字典（通用枚举维护）

### 3.2 入库管理
- 入库单（单据号、供应商、明细行：物料+数量+批次+效期+目标库位）
- 收货确认（登记实收数量）
- 上架：生成 WCS 任务并下发，待 WCS 回传完成后更新库存
- 入库单状态流转：新建 → 已收货 → 上架中 → 已完成 / 已取消

### 3.3 出库管理
- 出库单（单据号、客户、明细行：物料+数量+批次策略）
- 分配/拣货：按 FIFO/指定批次扣减库存，生成 WCS 任务下发
- 复核发运：确认出库数量，关闭单据
- 出库单状态流转：新建 → 分配中 → 已分配 → 拣货中 → 已发运 / 已取消

### 3.4 库存管理
- 实时库存查询（按物料/库位/批次多维过滤）
- 库存调整（增/减，需记录原因）
- 移库（库位 A → 库位 B，可选生成 WCS 任务）
- 盘点（盘点单 + 盘盈盘亏处理）

### 3.5 报表查询
- 库存余额报表
- 出入库流水报表

### 3.6 WCS 对接接口（对外 API）
- 接收 WCS 任务状态回调（任务完成/失败 → 更新单据状态 + 库存）
- 对外库存查询接口（供 WCS/ERP 调用）
- 预留：任务下发的抽象（通过配置的 WCS 地址 HTTP 调用）

### 3.7 前端管理后台（Vue3 + Element Plus）
- 技术栈：Vue3 + Vite + Element Plus + Pinia + Vue Router + Axios
- 布局：左侧菜单 + 顶部栏 + 内容区，统一主题
- 登录页（对接后端 Cookie 认证）
- 基础数据页面：库位管理、物料分类、物料管理、数据字典
- 入库页面：入库单列表、新建入库单、收货、上架
- 出库页面：出库单列表、新建出库单、分配/拣货、发运
- 库存页面：库存查询、库存调整、移库、盘点
- 报表页面：库存余额、出入库流水
- 通用：表格分页、表单校验、统一请求封装、错误提示、路由守卫

## 4. 非功能需求

| 项 | 要求 |
|---|---|
| 后端技术栈 | net8.0 + EF Core 8.0 + SQL Server（独立库 `KSKJ_WMS`） |
| 前端技术栈 | Vue3 + Vite + Element Plus + Pinia + Vue Router + Axios（独立工程 `KSKJ_WMS_Web`） |
| 架构形态 | 独立解决方案 `KSKJ_WMS.sln`，后端分层：WMS_Common / WMS_Model / WMS_Helper / WMS_IServices / WMS_Services / KSKJ_WMS(Api)；前端独立工程 |
| 分层约定 | 与 KSKJ_WCS 一致：统一 ResponseMessage、ResponseException、ErrorCode、GlobalExceptionFilter、BaseService/IBaseService、IDbContextFactory、EF 迁移 |
| 日志 | 走 LogHelper（落库，不写 txt）；区分 WMS 业务日志 |
| 重发 | 与 WCS 任务下发失败的消息走 retry_messages 表自动重发（复用 RetryRecorder/RetryService 约定） |
| 认证 | Cookie 认证（最简，与 WCS 一致），前端路由守卫 |
| 文档 | Swagger UI，接口有中文注释 |
| 可编译 | `dotnet build` 0 错误；`dotnet ef migrations` 可生成并应用；前端 `npm run build` 通过 |
| 跨域 | 后端配置 CORS 允许前端地址 |

## 5. 约束与假设

- 假设 SQL Server 本地实例可达，账号 `sa/123456`，新建库 `KSKJ_WMS`。
- WCS 对接接口仅做协议约定 + 模拟下发，真实 WMS↔WCS 联调需后续按实际 WCS API 地址配置。
- 所有金额、数量用 `decimal`；时间统一 UTC 存储。
- 表名与字段名沿用 snake_case（如 `wms_inbound_order`），与 WCS 风格一致。
- 不在本次实现前端。

## 6. 开放问题

- Q1：WMS 与 WCS 是否同端口部署？（假设独立端口，WMS 通过配置的 `WcsBaseUrl` 调用 WCS）
- Q2：是否需要租户/多仓库数据隔离？（假设单仓，不做租户）
- Q3：出库批次策略是否仅 FIFO？（假设 FIFO + 可手动指定批次）

> 以上 Q1-Q3 按假设实现，不影响核心闭环。

## 7. 验收标准

### 规则（可客观验证）

- **R1**：解决方案 `KSKJ_WMS.sln` 可在 `dotnet build` 下 0 错误编译通过。
- **R2**：存在 6 个分层项目（WMS_Common / WMS_Model / WMS_Helper / WMS_IServices / WMS_Services / KSKJ_WMS），引用关系与 WCS 一致。
- **R3**：`dotnet ef migrations add` 可生成迁移，`database update` 可在 `KSKJ_WMS` 库创建全部业务表（含 `__EFMigrationsHistory`）。
- **R4**：基础数据 4 类（库位、物料分类、物料、字典）均提供 CRUD 接口，Swagger 可见。
- **R5**：入库单支持新建→收货→上架→完成的状态流转，上架触发库存增加。
- **R6**：出库单支持新建→分配→发运的状态流转，发运触发库存扣减。
- **R7**：库存查询支持按物料、库位、批次过滤；库存调整/移库/盘点接口可正常工作。
- **R8**：存在 WCS 任务状态回调接口，调用后能更新对应单据状态。
- **R9**：所有 Controller 返回 `ResponseMessage` 统一结构；业务异常经 `GlobalExceptionFilter` 转换。
- **R10**：所有新增代码带 `nan_T 2026-09-09：` 注释；数据模型全部位于 WMS_Model 层。
- **R11**：前端工程 `KSKJ_WMS_Web` 存在，`npm install` 后 `npm run build` 通过；包含登录页与全部业务页面路由。
- **R12**：前端统一通过 Axios 封装调用后端，响应经 ResponseMessage 解析；未登录跳转登录页。

### 评分项（rubric）

- **U1 分层一致性（0-2）**：WMS 分层与 WCS 约定一致，Common 只放枚举/响应/异常，Model 只放实体，Helper 放 DbContext/工厂，IServices 放接口，Services 放实现，Api 放控制器/过滤器。阈值 ≥ 1.5。
- **U2 接口完整度（0-2）**：核心业务闭环（入库上架增库存、出库发运扣库存、库存查询、WCS 回调）端到端可跑通。阈值 ≥ 1.5。
- **U3 可维护性（0-2）**：服务继承 BaseService、接口继承 IBaseService，异常抛 ResponseException，无散落 try/catch，无 txt 日志。阈值 ≥ 1.5。
