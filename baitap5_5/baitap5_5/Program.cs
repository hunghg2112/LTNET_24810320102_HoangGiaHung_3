using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace InventoryManagementApp
{
    #region ==================== 1. DTO LAYER (DATA TRANSFER OBJECTS) ====================
    public class NhaCungCapDTO
    {
        public int MaNCC { get; set; }
        public string TenNCC { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
    }

    public class VatTuDTO
    {
        public string MaVatTu { get; set; }
        public string TenVatTu { get; set; }
        public string DVT { get; set; }
        public decimal SoLuongTon { get; set; }
    }

    public class PhieuKhoDTO
    {
        public int MaPhieu { get; set; }
        public string LoaiPhieu { get; set; } // "NHAP" hoặc "XUAT"
        public DateTime NgayLap { get; set; }
        public int? MaNCC { get; set; }
        public decimal TongTien { get; set; }
    }

    public class ChiTietPhieuKhoDTO
    {
        public int MaPhieu { get; set; }
        public string MaVatTu { get; set; }
        public string TenVatTu { get; set; }
        public string DVT { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
    #endregion

    #region ==================== 2. DAL LAYER (DATA ACCESS LAYER) ====================
    public class DatabaseAccess
    {
        protected static string connectionString = @"Server=localhost\SQLEXPRESS;Database=InventoryDB;Integrated Security=True;TrustServerCertificate=True;";
    }

    public class VatTuDAL : DatabaseAccess
    {
        public DataTable GetAll()
        {
            string query = "SELECT MaVatTu AS [Mã VT], TenVatTu AS [Tên vật tư], DVT AS [ĐVT], SoLuongTon AS [Số lượng tồn] FROM VatTu";
            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Tìm kiếm Vật tư 100% bằng SqlParameter chống SQL Injection
        public DataTable Search(string keyword)
        {
            string query = @"SELECT MaVatTu AS [Mã VT], TenVatTu AS [Tên vật tư], DVT AS [ĐVT], SoLuongTon AS [Số lượng tồn] 
                            FROM VatTu 
                            WHERE MaVatTu LIKE @Keyword OR TenVatTu LIKE @Keyword";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public List<VatTuDTO> GetLowStock(decimal threshold)
        {
            List<VatTuDTO> list = new List<VatTuDTO>();
            string query = "SELECT MaVatTu, TenVatTu, DVT, SoLuongTon FROM VatTu WHERE SoLuongTon <= @Threshold";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Threshold", threshold);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new VatTuDTO
                            {
                                MaVatTu = reader["MaVatTu"].ToString(),
                                TenVatTu = reader["TenVatTu"].ToString(),
                                DVT = reader["DVT"].ToString(),
                                SoLuongTon = Convert.ToDecimal(reader["SoLuongTon"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // Sử dụng ExecuteScalar tính tổng giá trị tồn kho
        // Sử dụng ExecuteScalar tính tổng giá trị tồn kho
        public decimal GetTotalInventoryValue()
        {
            // Bọc subquery vào bảng tạm (Derived Table) 'TempTable' trước khi dùng hàm SUM()
            string query = @"
        SELECT ISNULL(SUM(Val), 0)
        FROM (
            SELECT v.SoLuongTon * ISNULL(
                (SELECT TOP 1 DonGia 
                 FROM ChiTietPhieuKho 
                 WHERE MaVatTu = v.MaVatTu 
                 ORDER BY MaPhieu DESC), 50000) AS Val
            FROM VatTu v
        ) AS TempTable";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }
            }
        }
    }

    public class NhaCungCapDAL : DatabaseAccess
    {
        public List<NhaCungCapDTO> GetAll()
        {
            List<NhaCungCapDTO> list = new List<NhaCungCapDTO>();
            string query = "SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM NhaCungCap";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new NhaCungCapDTO
                            {
                                MaNCC = (int)reader["MaNCC"],
                                TenNCC = reader["TenNCC"].ToString(),
                                DienThoai = reader["DienThoai"]?.ToString(),
                                DiaChi = reader["DiaChi"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class PhieuKhoDAL : DatabaseAccess
    {
        // THỰC THI GIAO DỊCH SQLTRANSACTION XỬ LÝ NHẬP / XUẤT KHO ĐA BẢNG
        public bool SavePhieuKhoTransaction(PhieuKhoDTO phieu, List<ChiTietPhieuKhoDTO> details, out string errorMessage)
        {
            errorMessage = "";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // 1. Chèn bản ghi vào bảng PhieuKho
                    string queryPhieu = @"INSERT INTO PhieuKho (LoaiPhieu, NgayLap, MaNCC, TongTien) 
                                         VALUES (@LoaiPhieu, GETDATE(), @MaNCC, @TongTien); 
                                         SELECT SCOPE_IDENTITY();";
                    int maPhieuGenerated = 0;
                    using (SqlCommand cmdPhieu = new SqlCommand(queryPhieu, conn, trans))
                    {
                        cmdPhieu.Parameters.AddWithValue("@LoaiPhieu", phieu.LoaiPhieu);
                        cmdPhieu.Parameters.AddWithValue("@MaNCC", (object)phieu.MaNCC ?? DBNull.Value);
                        cmdPhieu.Parameters.AddWithValue("@TongTien", phieu.TongTien);
                        maPhieuGenerated = Convert.ToInt32(cmdPhieu.ExecuteScalar());
                    }

                    // 2. Chèn từng chi tiết và Cập nhật biến động tồn kho
                    foreach (var item in details)
                    {
                        // Kiểm tra nếu là XUẤT KHO -> Kiểm tra số lượng tồn kho hiện tại
                        if (phieu.LoaiPhieu == "XUAT")
                        {
                            string checkQuery = "SELECT SoLuongTon FROM VatTu WHERE MaVatTu = @MaVatTu";
                            decimal currentStock = 0;
                            using (SqlCommand cmdCheck = new SqlCommand(checkQuery, conn, trans))
                            {
                                cmdCheck.Parameters.AddWithValue("@MaVatTu", item.MaVatTu);
                                object res = cmdCheck.ExecuteScalar();
                                currentStock = res != null ? Convert.ToDecimal(res) : 0;
                            }

                            if (currentStock < item.SoLuong)
                            {
                                trans.Rollback(); // HỦY GIAO DỊCH NẾU KHÔNG ĐỦ HÀNG
                                errorMessage = $"Không đủ hàng trong kho! Vật tư '{item.TenVatTu}' ({item.MaVatTu}) hiện chỉ còn {currentStock} {item.DVT}, không đủ để xuất {item.SoLuong}.";
                                return false;
                            }
                        }

                        // Chèn vào ChiTietPhieuKho
                        string queryCT = @"INSERT INTO ChiTietPhieuKho (MaPhieu, MaVatTu, SoLuong, DonGia) 
                                          VALUES (@MaPhieu, @MaVatTu, @SoLuong, @DonGia)";
                        using (SqlCommand cmdCT = new SqlCommand(queryCT, conn, trans))
                        {
                            cmdCT.Parameters.AddWithValue("@MaPhieu", maPhieuGenerated);
                            cmdCT.Parameters.AddWithValue("@MaVatTu", item.MaVatTu);
                            cmdCT.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                            cmdCT.Parameters.AddWithValue("@DonGia", item.DonGia);
                            cmdCT.ExecuteNonQuery();
                        }

                        // Cập nhật kho VatTu
                        string updateStockQuery = phieu.LoaiPhieu == "NHAP"
                            ? "UPDATE VatTu SET SoLuongTon = SoLuongTon + @SoLuong WHERE MaVatTu = @MaVatTu"
                            : "UPDATE VatTu SET SoLuongTon = SoLuongTon - @SoLuong WHERE MaVatTu = @MaVatTu";

                        using (SqlCommand cmdStock = new SqlCommand(updateStockQuery, conn, trans))
                        {
                            cmdStock.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                            cmdStock.Parameters.AddWithValue("@MaVatTu", item.MaVatTu);
                            cmdStock.ExecuteNonQuery();
                        }
                    }

                    // 3. Hoàn tất giao dịch thành công
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi hệ thống: " + ex.Message;
                    return false;
                }
            }
        }
    }
    #endregion

    #region ==================== 3. BUS LAYER (BUSINESS LOGIC LAYER) ====================
    public class InventoryBUS
    {
        private VatTuDAL vatTuDAL = new VatTuDAL();
        private NhaCungCapDAL nccDAL = new NhaCungCapDAL();
        private PhieuKhoDAL phieuKhoDAL = new PhieuKhoDAL();

        public DataTable GetVatTuDataTable() => vatTuDAL.GetAll();
        public DataTable SearchVatTu(string keyword) => vatTuDAL.Search(keyword);
        public List<NhaCungCapDTO> GetNhaCungCapList() => nccDAL.GetAll();
        public List<VatTuDTO> GetLowStockVatTu(decimal threshold) => vatTuDAL.GetLowStock(threshold);
        public decimal GetTotalInventoryValue() => vatTuDAL.GetTotalInventoryValue();

        public bool SavePhieuKho(PhieuKhoDTO phieu, List<ChiTietPhieuKhoDTO> details, out string errorMessage)
        {
            if (details == null || details.Count == 0)
            {
                errorMessage = "Chưa có vật tư nào trong danh sách phiếu!";
                return false;
            }

            if (phieu.LoaiPhieu == "NHAP" && phieu.MaNCC == null)
            {
                errorMessage = "Phiếu nhập kho bắt buộc chọn Nhà cung cấp!";
                return false;
            }

            return phieuKhoDAL.SavePhieuKhoTransaction(phieu, details, out errorMessage);
        }
    }
    #endregion

    #region ==================== 4. GUI LAYER (GRAPHICAL USER INTERFACE) ====================
    public class frmInventoryMain : Form
    {
        private InventoryBUS bus = new InventoryBUS();

        // TAB 1: DANH MỤC VẬT TƯ
        private DataGridView dgvVatTu;
        private TextBox txtSearchVT;

        // TAB 2: LẬP PHIẾU NHẬP / XUẤT
        private RadioButton rdoNhap, rdoXuat;
        private ComboBox cboNCC, cboVatTu;
        private NumericUpDown numSoLuong, numDonGia;
        private Button btnAddItem, btnSavePhieu;
        private DataGridView dgvDetails;
        private Label lblTotalAmount;
        private List<ChiTietPhieuKhoDTO> currentDetails = new List<ChiTietPhieuKhoDTO>();

        // TAB 3: BÁO CÁO & CẢNH BÁO TỒN
        private DataGridView dgvLowStock;
        private Label lblTotalValue;
        private Button btnRefreshReport;

        public frmInventoryMain()
        {
            TaoGiaoDien();
            LoadDataTab1();
            LoadDataTab2();
            LoadDataTab3();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Quy Trình Xuất - Nhập Kho (Inventory Management 3-Tier)";
            this.Size = new Size(1050, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            TabPage tabVT = new TabPage("Danh mục Vật tư");
            TabPage tabPhieu = new TabPage("Lập Phiếu Nhập / Xuất Kho");
            TabPage tabReport = new TabPage("Cảnh báo Tồn kho & Báo cáo");

            // ---------------- TAB 1: DANH MỤC VẬT TƯ ----------------
            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 55 };
            pnlSearch.Controls.Add(new Label { Text = "Tìm kiếm Vật tư (Mã/Tên):", Location = new Point(15, 18), AutoSize = true });
            txtSearchVT = new TextBox { Location = new Point(180, 15), Width = 300 };
            txtSearchVT.TextChanged += (s, e) => dgvVatTu.DataSource = bus.SearchVatTu(txtSearchVT.Text.Trim());
            pnlSearch.Controls.Add(txtSearchVT);

            dgvVatTu = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            tabVT.Controls.Add(dgvVatTu);
            tabVT.Controls.Add(pnlSearch);

            // ---------------- TAB 2: LẬP PHIẾU NHẬP / XUẤT ----------------
            GroupBox gbPhieu = new GroupBox { Text = "Thông tin phiếu kho", Location = new Point(15, 15), Size = new Size(1000, 100) };
            rdoNhap = new RadioButton { Text = "PHIẾU NHẬP KHO", Location = new Point(20, 35), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkGreen };
            rdoXuat = new RadioButton { Text = "PHIẾU XUẤT KHO", Location = new Point(180, 35), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkRed };
            rdoNhap.CheckedChanged += RdoLoaiPhieu_CheckedChanged;
            gbPhieu.Controls.Add(rdoNhap); gbPhieu.Controls.Add(rdoXuat);

            gbPhieu.Controls.Add(new Label { Text = "Nhà cung cấp:", Location = new Point(360, 38), AutoSize = true });
            cboNCC = new ComboBox { Location = new Point(460, 35), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            gbPhieu.Controls.Add(cboNCC);

            tabPhieu.Controls.Add(gbPhieu);

            GroupBox gbItem = new GroupBox { Text = "Chi tiết mặt hàng", Location = new Point(15, 125), Size = new Size(1000, 90) };
            gbItem.Controls.Add(new Label { Text = "Vật tư:", Location = new Point(15, 35), AutoSize = true });
            cboVatTu = new ComboBox { Location = new Point(65, 32), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            gbItem.Controls.Add(cboVatTu);

            gbItem.Controls.Add(new Label { Text = "Số lượng:", Location = new Point(300, 35), AutoSize = true });
            numSoLuong = new NumericUpDown { Location = new Point(370, 32), Width = 90, Minimum = 1, Maximum = 10000, Value = 1 };
            gbItem.Controls.Add(numSoLuong);

            gbItem.Controls.Add(new Label { Text = "Đơn giá:", Location = new Point(480, 35), AutoSize = true });
            numDonGia = new NumericUpDown { Location = new Point(545, 32), Width = 130, Maximum = 100000000, Increment = 10000, Value = 50000 };
            gbItem.Controls.Add(numDonGia);

            btnAddItem = new Button { Text = "Thêm vào danh sách", Location = new Point(700, 28), Width = 150, Height = 35, BackColor = Color.LightSkyBlue };
            btnAddItem.Click += BtnAddItem_Click;
            gbItem.Controls.Add(btnAddItem);

            tabPhieu.Controls.Add(gbItem);

            dgvDetails = new DataGridView { Location = new Point(15, 225), Size = new Size(1000, 320), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            tabPhieu.Controls.Add(dgvDetails);

            lblTotalAmount = new Label { Text = "Tổng tiền: 0 VNĐ", Location = new Point(15, 560), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.DarkRed };
            tabPhieu.Controls.Add(lblTotalAmount);

            btnSavePhieu = new Button { Text = "LƯU PHIẾU KHO", Location = new Point(815, 555), Width = 200, Height = 45, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnSavePhieu.Click += BtnSavePhieu_Click;
            tabPhieu.Controls.Add(btnSavePhieu);

            // ---------------- TAB 3: BÁO CÁO & CẢNH BÁO TỒN ----------------
            Panel pnlReportTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            lblTotalValue = new Label { Text = "Tổng giá trị hàng tồn kho: 0 VNĐ", Location = new Point(15, 20), AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.DarkBlue };
            pnlReportTop.Controls.Add(lblTotalValue);

            btnRefreshReport = new Button { Text = "Làm mới báo cáo", Location = new Point(820, 15), Width = 160, Height = 38 };
            btnRefreshReport.Click += (s, e) => LoadDataTab3();
            pnlReportTop.Controls.Add(btnRefreshReport);

            tabReport.Controls.Add(pnlReportTop);

            GroupBox gbLowStock = new GroupBox { Text = "Danh sách Vật tư tồn kho dưới ngưỡng an toàn (<= 10)", Dock = DockStyle.Fill };
            dgvLowStock = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            gbLowStock.Controls.Add(dgvLowStock);
            tabReport.Controls.Add(gbLowStock);

            tabControl.TabPages.Add(tabVT);
            tabControl.TabPages.Add(tabPhieu);
            tabControl.TabPages.Add(tabReport);
            this.Controls.Add(tabControl);
        }

        // --- TAB 1 LOGIC ---
        private void LoadDataTab1()
        {
            dgvVatTu.DataSource = bus.GetVatTuDataTable();
        }

        // --- TAB 2 LOGIC ---
        private void LoadDataTab2()
        {
            cboNCC.DataSource = bus.GetNhaCungCapList();
            cboNCC.DisplayMember = "TenNCC";
            cboNCC.ValueMember = "MaNCC";

            DataTable dtVT = bus.GetVatTuDataTable();
            cboVatTu.DataSource = dtVT;
            cboVatTu.DisplayMember = "Tên vật tư";
            cboVatTu.ValueMember = "Mã VT";

            RefreshDetailsGrid();
        }

        private void RdoLoaiPhieu_CheckedChanged(object sender, EventArgs e)
        {
            cboNCC.Enabled = rdoNhap.Checked;
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (cboVatTu.SelectedValue == null) return;

            DataRowView row = cboVatTu.SelectedItem as DataRowView;
            string maVT = row["Mã VT"].ToString();
            string tenVT = row["Tên vật tư"].ToString();
            string dvt = row["ĐVT"].ToString();
            decimal qty = numSoLuong.Value;
            decimal price = numDonGia.Value;

            // Nếu đã tồn tại trong danh sách tạm -> Cộng dồn số lượng
            var existItem = currentDetails.Find(x => x.MaVatTu == maVT);
            if (existItem != null)
            {
                existItem.SoLuong += qty;
                existItem.DonGia = price;
            }
            else
            {
                currentDetails.Add(new ChiTietPhieuKhoDTO
                {
                    MaVatTu = maVT,
                    TenVatTu = tenVT,
                    DVT = dvt,
                    SoLuong = qty,
                    DonGia = price
                });
            }

            RefreshDetailsGrid();
        }

        private void RefreshDetailsGrid()
        {
            dgvDetails.DataSource = null;
            dgvDetails.DataSource = currentDetails;

            decimal total = 0;
            foreach (var item in currentDetails)
            {
                total += item.ThanhTien;
            }

            lblTotalAmount.Text = $"Tổng tiền: {total:N0} VNĐ";
        }

        private void BtnSavePhieu_Click(object sender, EventArgs e)
        {
            PhieuKhoDTO phieu = new PhieuKhoDTO
            {
                LoaiPhieu = rdoNhap.Checked ? "NHAP" : "XUAT",
                MaNCC = rdoNhap.Checked ? (int?)cboNCC.SelectedValue : null,
                TongTien = 0
            };

            foreach (var d in currentDetails) phieu.TongTien += d.ThanhTien;

            if (bus.SavePhieuKho(phieu, currentDetails, out string errorMsg))
            {
                MessageBox.Show("Lưu phiếu kho và cập nhật tồn kho thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reset Form
                currentDetails.Clear();
                RefreshDetailsGrid();
                LoadDataTab1();
                LoadDataTab3();
            }
            else
            {
                MessageBox.Show(errorMsg, "Lỗi giao dịch (Rollback)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- TAB 3 LOGIC ---
        private void LoadDataTab3()
        {
            dgvLowStock.DataSource = bus.GetLowStockVatTu(10);
            decimal totalVal = bus.GetTotalInventoryValue();
            lblTotalValue.Text = $"Tổng giá trị hàng tồn kho: {totalVal:N0} VNĐ";
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmInventoryMain());
        }
    }
    #endregion
}