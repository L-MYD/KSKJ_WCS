# WMS 系统 — 审查记录

> 审查日期：2026-09-09

## 验收标准核查（Reviewer 独立验证）

| 编号 | 类型 | 结果 | 证据 |
|---|---|---|---|
| R1 | rule | ✅ pass | `dotnet build KSKJ_WMS.csproj` 0 错误（全部 6 个项目编译通过） |
| R2 | rule | ✅ pass | 存在 WMS_Common / WMS_Model / WMS_Helper / WMS_IServices / WMS_Services / KSKJ_WMS 六个项目，引用关系：Api→IServices+Services；Services→IServices+Helper+Model；Helper→Common+Model；IServices→Common+Model |
| R3 | rule | ✅ pass | `dotnet ef migrations add InitialCreate` + `database update` 成功；KSKJ_WMS 库共 21 张表（含 __EFMigrationsHistory） |
| R4 | rule | ✅ pass | Swagger 可见 Location / MaterialCategory / Material / DataDictionary 四组 CRUD 接口 |
| R5 | rule | ✅ pass | 实测：创建入库单→收货→上架，库存从 0 增至 100 |
| R6 | rule | ✅ pass | 实测：创建出库单→分配，库存从 100 减至 70 |
| R7 | rule | ✅ pass | 库存查询按 materialId 过滤正常；库存调整/移库/盘点接口已实现并经编译验证 |
| R8 | rule | ✅ pass | `POST /api/wcs/callback` 调用后返回"出库单状态已更新"，状态由拣货中变为已发运 |
| R9 | rule | ✅ pass | 所有 Controller 返回 ResponseMessage；业务异常经 GlobalExceptionFilter 转换（实测"库存不足"返回 code=400 + message） |
| R10 | rule | ✅ pass | 全部新增代码带 `nan_T 2026-09-09：` 注释；数据模型全部位于 WMS_Model/WMSModel |
| R11 | rule | ✅ pass | KSKJ_WMS_Web 工程存在，`npm run build` 成功（✓ built in 7.18s），含登录页 + 全部业务路由 |
| R12 | rule | ✅ pass | Axios 封装统一解析 ResponseMessage.code；401 跳转登录页；实测前端代理 `/api/login` 返回 code=200 |

### 评分项

| 编号 | 维度 | 得分 | 阈值 | 说明 |
|---|---|---|---|---|
| U1 | 分层一致性 | 2.0 | ≥1.5 | 严格遵循 WCS 分层约定，Common/Model/Helper/IServices/Services/Api 职责清晰 |
| U2 | 接口完整度 | 1.8 | ≥1.5 | 入库上架增库存、出库发运扣库存、库存查询、WCS 回调端到端跑通；调整/移库/盘点已实现 |
| U3 | 可维护性 | 1.8 | ≥1.5 | 服务继承 BaseService、接口继承 IBaseService；异常抛 ResponseException；无 txt 日志；无散落 try/catch |

## 端到端测试证据

```
matId=2  inboundId=2
receive: code=200
putaway: code=200
入库后库存 qty=100.0000
outboundId=2
allocate: code=200
出库后库存 qty=70.0000
wcs回调: code=200 msg=出库单状态已更新
```

## 审查结论

**pass** — 全部 12 条规则通过，3 项评分均达标。无待修复问题。

## 已知限制（非阻塞）

- WCS 任务下发为模拟实现（WcsBaseUrl 为空时跳过），真实联调需配置实际 WCS 地址。
- 前端为单页管理后台，移动端/PDA 界面未实现（符合非目标）。
- 权限为最简 Cookie 认证，无细粒度角色权限（符合非目标）。
