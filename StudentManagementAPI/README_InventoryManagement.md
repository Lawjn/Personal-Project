# Hệ thống Quản lý Tồn kho và Bán hàng - Flow 2

## Mô tả
Hệ thống quản lý tồn kho và bán hàng xe điện theo Flow 2 với các chức năng:

### Người dùng: Backend - Tồn kho & Bán hàng (Flow 2)
- **Controller**: Service cho Inventory, SellFromInventory
- **API nhập kho, tồn kho khi bán**
- **Stored Procedure sp.SellFromInventory**
- **Đồng bộ tồn kho giữa Dealer - Hãng**

### Người dùng: Backend - Báo cáo & Dự báo (Flow 2)
- **Controller**: Service cho ReportLog, Forecast
- **Tạo báo cáo doanh số tồn kho, phân phối**
- **Đề xuất mua nhập dựa trên forecast**
- **Export báo cáo (Excel/PDF)**

## Cấu trúc Database

### Models
1. **VehicleModel** - Danh mục xe điện (mẫu, phiên bản, màu sắc)
2. **Inventory** - Quản lý tồn kho theo từng mẫu xe
3. **SellFromInventory** - Ghi nhận bán hàng từ tồn kho
4. **ReportLog** - Lưu trữ lịch sử báo cáo
5. **Forecast** - Dự báo nhu cầu và doanh số

### Relationships
- VehicleModel (1) → (N) Inventory
- Inventory (1) → (N) SellFromInventory
- VehicleModel (1) → (N) Forecast

## API Endpoints

### VehicleModel Management
- `GET /api/VehicleModel` - Danh sách mẫu xe
- `GET /api/VehicleModel/{id}` - Chi tiết mẫu xe
- `GET /api/VehicleModel/with-inventory` - Mẫu xe với thông tin tồn kho
- `POST /api/VehicleModel` - Tạo mẫu xe mới
- `PUT /api/VehicleModel/{id}` - Cập nhật mẫu xe
- `DELETE /api/VehicleModel/{id}` - Xóa mẫu xe

### Inventory Management
- `GET /api/Inventory` - Danh sách tồn kho
- `GET /api/Inventory/{id}` - Chi tiết tồn kho
- `GET /api/Inventory/low-stock/{threshold}` - Hàng sắp hết
- `POST /api/Inventory` - Nhập kho
- `PUT /api/Inventory/{id}` - Cập nhật tồn kho
- `DELETE /api/Inventory/{id}` - Xóa tồn kho

### Sales Management
- `GET /api/SellFromInventory` - Lịch sử bán hàng
- `GET /api/SellFromInventory/{id}` - Chi tiết giao dịch
- `GET /api/SellFromInventory/sales-summary` - Tóm tắt doanh số
- `GET /api/SellFromInventory/top-selling` - Xe bán chạy
- `POST /api/SellFromInventory` - Bán hàng từ tồn kho
- `DELETE /api/SellFromInventory/{id}` - Hủy giao dịch

### Reports & Forecasting
- `POST /api/Reports/generate-sales-report` - Tạo báo cáo doanh số
- `POST /api/Reports/generate-forecast` - Tạo dự báo
- `GET /api/Reports/report-logs` - Lịch sử báo cáo
- `GET /api/Reports/forecasts` - Danh sách dự báo
- `GET /api/Reports/inventory-status` - Tình trạng tồn kho

## Tính năng chính

### 1. Quản lý Danh mục xe
- Thêm/sửa/xóa thông tin xe (tên, hãng, màu sắc, giá)
- Xem danh sách xe kèm thông tin tồn kho

### 2. Quản lý Tồn kho
- Nhập kho: Cập nhật số lượng và giá nhập
- Theo dõi tồn kho theo thời gian thực
- Cảnh báo hàng sắp hết
- Lịch sử nhập/xuất kho

### 3. Bán hàng từ Tồn kho
- Bán hàng với tự động trừ tồn kho
- Ghi nhận thông tin khách hàng
- Tính toán lợi nhuận tự động
- Hủy giao dịch và khôi phục tồn kho

### 4. Báo cáo và Phân tích
- Báo cáo doanh số theo khoảng thời gian
- Phân tích xe bán chạy nhất
- Báo cáo lợi nhuận chi tiết
- Tình trạng tồn kho tổng quan

### 5. Dự báo (Forecast)
- Dự báo nhu cầu dựa trên lịch sử bán hàng
- Tính toán xu hướng tăng/giảm
- Đề xuất nhập hàng
- Đánh giá độ tin cậy của dự báo

## Cài đặt và Chạy

### Yêu cầu
- .NET 8.0 SDK
- SQL Server hoặc SQL Server Express
- Entity Framework Core

### Cài đặt
```bash
# Clone repository
cd StudentManagementAPI

# Cài đặt dependencies
dotnet restore

# Cập nhật connection string trong appsettings.json
# Tạo database và chạy migration
dotnet ef database update

# Chạy ứng dụng
dotnet run
```

### Testing
Sử dụng file `InventoryManagement.http` để test các API với VS Code REST Client hoặc Postman.

## Luồng sử dụng điển hình

1. **Tạo danh mục xe** → VehicleModel
2. **Nhập hàng vào kho** → Inventory
3. **Bán hàng từ kho** → SellFromInventory (tự động trừ tồn kho)
4. **Theo dõi tồn kho** → Kiểm tra hàng sắp hết
5. **Tạo báo cáo** → Phân tích doanh số và lợi nhuận
6. **Dự báo nhu cầu** → Lập kế hoạch nhập hàng

## Các tính năng nâng cao

- **Transaction Safety**: Sử dụng database transaction cho bán hàng
- **Automatic Inventory Update**: Tự động cập nhật tồn kho khi bán
- **Profit Calculation**: Tính lợi nhuận tự động
- **Trend Analysis**: Phân tích xu hướng bán hàng
- **Low Stock Alerts**: Cảnh báo hàng sắp hết
- **Sales Cancellation**: Hủy giao dịch và khôi phục tồn kho

## Database Schema
Các bảng được tạo tự động thông qua Entity Framework Migration:
- VehicleModels
- Inventories  
- SellFromInventories
- ReportLogs
- Forecasts

Sử dụng `dotnet ef migrations add [MigrationName]` để tạo migration mới khi thay đổi model.