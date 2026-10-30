using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace HotelManagementApp
{
    #region ==================== 1. DTO LAYER (DATA TRANSFER OBJECTS) ====================
    public class PhongDTO
    {
        public string SoPhong { get; set; }
        public string LoaiPhong { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string TrangThai { get; set; }
    }

    public class KhachHangDTO
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; }
        public string CCCD { get; set; }
        public string SoDienThoai { get; set; }
    }

    public class DichVuDTO
    {
        public int MaDV { get; set; }
        public string TenDV { get; set; }
        public decimal DonGia { get; set; }
    }

    public class PhieuDatPhongDTO
    {
        public int MaPhieu { get; set; }
        public string SoPhong { get; set; }
        public int MaKH { get; set; }
        public DateTime NgayNhanPhong { get; set; }
        public DateTime? NgayTraPhong { get; set; }
        public decimal TongTienThanhToan { get; set; }
    }

    public class SuDungDichVuDTO
    {
        public int MaPhieu { get; set; }
        public int MaDV { get; set; }
        public int SoLuong { get; set; }
        public DateTime ThoiGian { get; set; }
    }
    #endregion

    #region ==================== 2. DAL LAYER (DATA ACCESS LAYER) ====================
    public class DatabaseAccess
    {
        protected static string connectionString = @"Server=localhost\SQLEXPRESS;Database=HotelDB;Integrated Security=True;TrustServerCertificate=True;";
    }

    public class KhachHangDAL : DatabaseAccess
    {
        public KhachHangDTO GetByCCCD(string cccd)
        {
            string query = "SELECT MaKH, HoTen, CCCD, SoDienThoai FROM KhachHang WHERE CCCD = @CCCD";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CCCD", cccd);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new KhachHangDTO
                            {
                                MaKH = (int)reader["MaKH"],
                                HoTen = reader["HoTen"].ToString(),
                                CCCD = reader["CCCD"].ToString(),
                                SoDienThoai = reader["SoDienThoai"]?.ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public int Insert(KhachHangDTO kh)
        {
            string query = "INSERT INTO KhachHang (HoTen, CCCD, SoDienThoai) VALUES (@HoTen, @CCCD, @SĐT); SELECT SCOPE_IDENTITY();";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@HoTen", kh.HoTen);
                    cmd.Parameters.AddWithValue("@CCCD", kh.CCCD);
                    cmd.Parameters.AddWithValue("@SĐT", kh.SoDienThoai ?? "");
                    conn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }

    public class PhongDAL : DatabaseAccess
    {
        public DataTable GetAllToDataTable()
        {
            string query = "SELECT SoPhong AS [Số phòng], LoaiPhong AS [Loại phòng], GiaTheoGio AS [Giá/Giờ], TrangThai AS [Trạng thái] FROM Phong";
            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public List<PhongDTO> GetRoomsByStatus(string status)
        {
            List<PhongDTO> list = new List<PhongDTO>();
            string query = "SELECT SoPhong, LoaiPhong, GiaTheoGio, TrangThai FROM Phong WHERE TrangThai = @Status";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PhongDTO
                            {
                                SoPhong = reader["SoPhong"].ToString(),
                                LoaiPhong = reader["LoaiPhong"].ToString(),
                                GiaTheoGio = Convert.ToDecimal(reader["GiaTheoGio"]),
                                TrangThai = reader["TrangThai"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class DichVuDAL : DatabaseAccess
    {
        public List<DichVuDTO> GetAll()
        {
            List<DichVuDTO> list = new List<DichVuDTO>();
            string query = "SELECT MaDV, TenDV, DonGia FROM DichVu";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DichVuDTO
                            {
                                MaDV = (int)reader["MaDV"],
                                TenDV = reader["TenDV"].ToString(),
                                DonGia = Convert.ToDecimal(reader["DonGia"])
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class HotelDAL : DatabaseAccess
    {
        public bool CheckIn(string soPhong, int maKH)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // 1. Tạo phiếu đặt
                    string q1 = "INSERT INTO PhieuDatPhong (SoPhong, MaKH, NgayNhanPhong) VALUES (@SoPhong, @MaKH, GETDATE())";
                    using (SqlCommand cmd1 = new SqlCommand(q1, conn, trans))
                    {
                        cmd1.Parameters.AddWithValue("@SoPhong", soPhong);
                        cmd1.Parameters.AddWithValue("@MaKH", maKH);
                        cmd1.ExecuteNonQuery();
                    }

                    // 2. Chuyển trạng thái phòng -> 'Đang ở'
                    string q2 = "UPDATE Phong SET TrangThai = N'Đang ở' WHERE SoPhong = @SoPhong";
                    using (SqlCommand cmd2 = new SqlCommand(q2, conn, trans))
                    {
                        cmd2.Parameters.AddWithValue("@SoPhong", soPhong);
                        cmd2.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch
                {
                    trans.Rollback();
                    return false;
                }
            }
        }

        public PhieuDatPhongDTO GetActivePhieuByRoom(string soPhong)
        {
            string query = "SELECT MaPhieu, SoPhong, MaKH, NgayNhanPhong FROM PhieuDatPhong WHERE SoPhong = @SoPhong AND NgayTraPhong IS NULL";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SoPhong", soPhong);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PhieuDatPhongDTO
                            {
                                MaPhieu = (int)reader["MaPhieu"],
                                SoPhong = reader["SoPhong"].ToString(),
                                MaKH = (int)reader["MaKH"],
                                NgayNhanPhong = Convert.ToDateTime(reader["NgayNhanPhong"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void AddServiceUsage(SuDungDichVuDTO sd)
        {
            string query = "INSERT INTO SuDungDichVu (MaPhieu, MaDV, SoLuong, ThoiGian) VALUES (@MaPhieu, @MaDV, @SoLuong, GETDATE())";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhieu", sd.MaPhieu);
                    cmd.Parameters.AddWithValue("@MaDV", sd.MaDV);
                    cmd.Parameters.AddWithValue("@SoLuong", sd.SoLuong);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable GetServiceUsageDetails(int maPhieu)
        {
            string query = @"SELECT dv.TenDV AS [Tên dịch vụ], sd.SoLuong AS [Số lượng], dv.DonGia AS [Đơn giá], 
                            (sd.SoLuong * dv.DonGia) AS [Thành tiền], sd.ThoiGian AS [Thời gian]
                            FROM SuDungDichVu sd
                            JOIN DichVu dv ON sd.MaDV = dv.MaDV
                            WHERE sd.MaPhieu = @MaPhieu";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhieu", maPhieu);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // Check-out thực thi SqlTransaction
        public bool CheckOutWithTransaction(int maPhieu, string soPhong, decimal tongTien)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // 1. Cập nhật phiếu đặt
                    string q1 = "UPDATE PhieuDatPhong SET NgayTraPhong = GETDATE(), TongTienThanhToan = @TongTien WHERE MaPhieu = @MaPhieu";
                    using (SqlCommand cmd1 = new SqlCommand(q1, conn, trans))
                    {
                        cmd1.Parameters.AddWithValue("@TongTien", tongTien);
                        cmd1.Parameters.AddWithValue("@MaPhieu", maPhieu);
                        cmd1.ExecuteNonQuery();
                    }

                    // 2. Chuyển phòng sang 'Đang dọn dẹp'
                    string q2 = "UPDATE Phong SET TrangThai = N'Đang dọn dẹp' WHERE SoPhong = @SoPhong";
                    using (SqlCommand cmd2 = new SqlCommand(q2, conn, trans))
                    {
                        cmd2.Parameters.AddWithValue("@SoPhong", soPhong);
                        cmd2.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch
                {
                    trans.Rollback();
                    return false;
                }
            }
        }
    }
    #endregion

    #region ==================== 3. BUS LAYER (BUSINESS LOGIC LAYER) ====================
    public class HotelBUS
    {
        private KhachHangDAL khDAL = new KhachHangDAL();
        private PhongDAL phongDAL = new PhongDAL();
        private DichVuDAL dvDAL = new DichVuDAL();
        private HotelDAL hotelDAL = new HotelDAL();

        public DataTable GetRoomsDataTable() => phongDAL.GetAllToDataTable();
        public List<PhongDTO> GetEmptyRooms() => phongDAL.GetRoomsByStatus("Trống");
        public List<PhongDTO> GetOccupiedRooms() => phongDAL.GetRoomsByStatus("Đang ở");
        public List<DichVuDTO> GetServices() => dvDAL.GetAll();

        public KhachHangDTO FindCustomerByCCCD(string cccd) => khDAL.GetByCCCD(cccd);

        public int RegisterOrGetCustomer(string hoTen, string cccd, string sdt)
        {
            KhachHangDTO kh = khDAL.GetByCCCD(cccd);
            if (kh != null) return kh.MaKH;

            return khDAL.Insert(new KhachHangDTO { HoTen = hoTen, CCCD = cccd, SoDienThoai = sdt });
        }

        public bool CheckIn(string soPhong, int maKH) => hotelDAL.CheckIn(soPhong, maKH);

        public PhieuDatPhongDTO GetActivePhieu(string soPhong) => hotelDAL.GetActivePhieuByRoom(soPhong);

        public void AddServiceUsage(int maPhieu, int maDV, int soLuong)
        {
            hotelDAL.AddServiceUsage(new SuDungDichVuDTO { MaPhieu = maPhieu, MaDV = maDV, SoLuong = soLuong });
        }

        public DataTable GetServiceDetails(int maPhieu) => hotelDAL.GetServiceUsageDetails(maPhieu);

        public bool CheckOut(int maPhieu, string soPhong, decimal tongTien) => hotelDAL.CheckOutWithTransaction(maPhieu, soPhong, tongTien);
    }
    #endregion

    #region ==================== 4. GUI LAYER (GRAPHICAL USER INTERFACE) ====================
    public class frmHotelMain : Form
    {
        private HotelBUS bus = new HotelBUS();

        // TAB 1: SEARCH DISCONNECTED
        private DataGridView dgvRooms;
        private ComboBox cboFilterType, cboFilterStatus;
        private DataTable dtRooms;
        private DataView dvRooms;

        // TAB 2: CHECK-IN
        private TextBox txtCCCD, txtHoTen, txtSDT;
        private ComboBox cboEmptyRooms;
        private Button btnCheckCCCD, btnCheckIn;
        private int currentCustomerMaKH = -1;

        // TAB 3: SERVICE & CHECK-OUT
        private ComboBox cboOccupiedRooms, cboServices;
        private NumericUpDown numServiceQty;
        private Button btnAddService, btnCalculate, btnCheckOut;
        private DataGridView dgvUsedServices;
        private Label lblCheckInTime, lblRoomPrice, lblTotalAmount;
        private PhieuDatPhongDTO currentPhieu;
        private decimal currentHourlyPrice = 0;

        public frmHotelMain()
        {
            TaoGiaoDien();
            LoadDataTab1();
            LoadDataTab2();
            LoadDataTab3();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Tiện Ích Dịch Vụ & Khách Sạn (3-Tier)";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            TabPage tabRooms = new TabPage("Lọc & Sơ đồ Phòng");
            TabPage tabCheckIn = new TabPage("Check-in (Nhận phòng)");
            TabPage tabCheckout = new TabPage("Dịch vụ & Check-out");

            // ---------------- TAB 1: FILTER DISCONNECTED ----------------
            Panel pnlFilter = new Panel { Dock = DockStyle.Top, Height = 50 };
            pnlFilter.Controls.Add(new Label { Text = "Loại phòng:", Location = new Point(15, 15), AutoSize = true });
            cboFilterType = new ComboBox { Location = new Point(95, 12), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterType.Items.AddRange(new object[] { "Tất cả", "Đơn", "Đôi", "VIP" });
            cboFilterType.SelectedIndex = 0;
            cboFilterType.SelectedIndexChanged += FilterRooms;
            pnlFilter.Controls.Add(cboFilterType);

            pnlFilter.Controls.Add(new Label { Text = "Trạng thái:", Location = new Point(250, 15), AutoSize = true });
            cboFilterStatus = new ComboBox { Location = new Point(330, 12), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterStatus.Items.AddRange(new object[] { "Tất cả", "Trống", "Đang ở", "Đang dọn dẹp" });
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += FilterRooms;
            pnlFilter.Controls.Add(cboFilterStatus);

            dgvRooms = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            tabRooms.Controls.Add(dgvRooms);
            tabRooms.Controls.Add(pnlFilter);

            // ---------------- TAB 2: CHECK-IN ----------------
            GroupBox gbKH = new GroupBox { Text = "Thông tin khách hàng", Location = new Point(20, 20), Size = new Size(450, 250) };
            gbKH.Controls.Add(new Label { Text = "Số CCCD:", Location = new Point(20, 35), AutoSize = true });
            txtCCCD = new TextBox { Location = new Point(120, 32), Width = 200 };
            gbKH.Controls.Add(txtCCCD);

            btnCheckCCCD = new Button { Text = "Tra cứu", Location = new Point(330, 30), Width = 90, Height = 28 };
            btnCheckCCCD.Click += BtnCheckCCCD_Click;
            gbKH.Controls.Add(btnCheckCCCD);

            gbKH.Controls.Add(new Label { Text = "Họ và tên:", Location = new Point(20, 85), AutoSize = true });
            txtHoTen = new TextBox { Location = new Point(120, 82), Width = 300 };
            gbKH.Controls.Add(txtHoTen);

            gbKH.Controls.Add(new Label { Text = "Số điện thoại:", Location = new Point(20, 135), AutoSize = true });
            txtSDT = new TextBox { Location = new Point(120, 132), Width = 300 };
            gbKH.Controls.Add(txtSDT);

            tabCheckIn.Controls.Add(gbKH);

            GroupBox gbCI = new GroupBox { Text = "Chọn phòng nhận", Location = new Point(500, 20), Size = new Size(450, 250) };
            gbCI.Controls.Add(new Label { Text = "Phòng trống:", Location = new Point(20, 35), AutoSize = true });
            cboEmptyRooms = new ComboBox { Location = new Point(120, 32), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            gbCI.Controls.Add(cboEmptyRooms);

            btnCheckIn = new Button { Text = "XÁC NHẬN CHECK-IN", Location = new Point(120, 100), Width = 250, Height = 45, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnCheckIn.Click += BtnCheckIn_Click;
            gbCI.Controls.Add(btnCheckIn);

            tabCheckIn.Controls.Add(gbCI);

            // ---------------- TAB 3: SERVICE & CHECK-OUT ----------------
            GroupBox gbOrder = new GroupBox { Text = "Gọi Dịch vụ", Location = new Point(20, 20), Size = new Size(450, 520) };
            gbOrder.Controls.Add(new Label { Text = "Chọn phòng đang ở:", Location = new Point(15, 30), AutoSize = true });
            cboOccupiedRooms = new ComboBox { Location = new Point(155, 27), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cboOccupiedRooms.SelectedIndexChanged += CboOccupiedRooms_SelectedIndexChanged;
            gbOrder.Controls.Add(cboOccupiedRooms);

            lblCheckInTime = new Label { Text = "Giờ nhận: --", Location = new Point(15, 70), AutoSize = true };
            gbOrder.Controls.Add(lblCheckInTime);

            gbOrder.Controls.Add(new Label { Text = "Chọn dịch vụ:", Location = new Point(15, 115), AutoSize = true });
            cboServices = new ComboBox { Location = new Point(115, 112), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            gbOrder.Controls.Add(cboServices);

            gbOrder.Controls.Add(new Label { Text = "SL:", Location = new Point(305, 115), AutoSize = true });
            numServiceQty = new NumericUpDown { Location = new Point(335, 112), Width = 50, Minimum = 1, Value = 1 };
            gbOrder.Controls.Add(numServiceQty);

            btnAddService = new Button { Text = "Thêm Dịch Vụ", Location = new Point(115, 155), Width = 180, Height = 32, BackColor = Color.LightSkyBlue };
            btnAddService.Click += BtnAddService_Click;
            gbOrder.Controls.Add(btnAddService);

            dgvUsedServices = new DataGridView { Location = new Point(15, 205), Size = new Size(415, 290), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            gbOrder.Controls.Add(dgvUsedServices);

            tabCheckout.Controls.Add(gbOrder);

            GroupBox gbBill = new GroupBox { Text = "Thanh toán & Xuất hóa đơn", Location = new Point(490, 20), Size = new Size(460, 520) };
            lblRoomPrice = new Label { Text = "Giá phòng: 0 VNĐ/giờ", Location = new Point(20, 40), AutoSize = true, Font = new Font("Segoe UI", 11F) };
            gbBill.Controls.Add(lblRoomPrice);

            lblTotalAmount = new Label { Text = "Tổng tiền: 0 VNĐ", Location = new Point(20, 100), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.DarkRed };
            gbBill.Controls.Add(lblTotalAmount);

            btnCalculate = new Button { Text = "TÍNH TỔNG TIỀN", Location = new Point(20, 170), Width = 200, Height = 40, BackColor = Color.Khaki };
            btnCalculate.Click += BtnCalculate_Click;
            gbBill.Controls.Add(btnCalculate);

            btnCheckOut = new Button { Text = "THANH TOÁN CHECK-OUT", Location = new Point(230, 170), Width = 210, Height = 40, BackColor = Color.LightCoral, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnCheckOut.Click += BtnCheckOut_Click;
            gbBill.Controls.Add(btnCheckOut);

            tabCheckout.Controls.Add(gbBill);

            tabControl.TabPages.Add(tabRooms);
            tabControl.TabPages.Add(tabCheckIn);
            tabControl.TabPages.Add(tabCheckout);
            this.Controls.Add(tabControl);
        }

        // --- TAB 1 LOGIC ---
        private void LoadDataTab1()
        {
            dtRooms = bus.GetRoomsDataTable();
            dvRooms = new DataView(dtRooms);
            dgvRooms.DataSource = dvRooms;
        }

        private void FilterRooms(object sender, EventArgs e)
        {
            string filter = "";
            if (cboFilterType.SelectedIndex > 0)
                filter += $"[Loại phòng] = '{cboFilterType.SelectedItem}'";

            if (cboFilterStatus.SelectedIndex > 0)
            {
                if (filter.Length > 0) filter += " AND ";
                filter += $"[Trạng thái] = '{cboFilterStatus.SelectedItem}'";
            }

            dvRooms.RowFilter = filter;
        }

        // --- TAB 2 LOGIC ---
        private void LoadDataTab2()
        {
            cboEmptyRooms.DataSource = bus.GetEmptyRooms();
            cboEmptyRooms.DisplayMember = "SoPhong";
            cboEmptyRooms.ValueMember = "SoPhong";
        }

        private void BtnCheckCCCD_Click(object sender, EventArgs e)
        {
            string cccd = txtCCCD.Text.Trim();
            if (string.IsNullOrEmpty(cccd))
            {
                MessageBox.Show("Vui lòng nhập số CCCD!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            KhachHangDTO kh = bus.FindCustomerByCCCD(cccd);
            if (kh != null)
            {
                currentCustomerMaKH = kh.MaKH;
                txtHoTen.Text = kh.HoTen;
                txtSDT.Text = kh.SoDienThoai;
                MessageBox.Show("Tìm thấy thông tin khách hàng cũ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                currentCustomerMaKH = -1;
                txtHoTen.Clear();
                txtSDT.Clear();
                MessageBox.Show("Khách hàng mới! Vui lòng nhập Họ tên và SĐT.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCheckIn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCCCD.Text) || string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin khách hàng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboEmptyRooms.SelectedValue == null)
            {
                MessageBox.Show("Không có phòng trống để nhận!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int maKH = bus.RegisterOrGetCustomer(txtHoTen.Text.Trim(), txtCCCD.Text.Trim(), txtSDT.Text.Trim());
            string soPhong = cboEmptyRooms.SelectedValue.ToString();

            if (bus.CheckIn(soPhong, maKH))
            {
                MessageBox.Show($"Check-in phòng {soPhong} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh Data
                LoadDataTab1();
                LoadDataTab2();
                LoadDataTab3();
                txtCCCD.Clear(); txtHoTen.Clear(); txtSDT.Clear();
            }
        }

        // --- TAB 3 LOGIC ---
        private void LoadDataTab3()
        {
            cboOccupiedRooms.DataSource = bus.GetOccupiedRooms();
            cboOccupiedRooms.DisplayMember = "SoPhong";
            cboOccupiedRooms.ValueMember = "SoPhong";

            cboServices.DataSource = bus.GetServices();
            cboServices.DisplayMember = "TenDV";
            cboServices.ValueMember = "MaDV";
        }

        private void CboOccupiedRooms_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboOccupiedRooms.SelectedItem is PhongDTO room)
            {
                currentPhieu = bus.GetActivePhieu(room.SoPhong);
                currentHourlyPrice = room.GiaTheoGio;
                lblRoomPrice.Text = $"Giá phòng: {currentHourlyPrice:N0} VNĐ/giờ";

                if (currentPhieu != null)
                {
                    lblCheckInTime.Text = $"Giờ nhận: {currentPhieu.NgayNhanPhong:dd/MM/yyyy HH:mm}";
                    dgvUsedServices.DataSource = bus.GetServiceDetails(currentPhieu.MaPhieu);
                    if (dgvUsedServices.Columns["Đơn giá"] != null) dgvUsedServices.Columns["Đơn giá"].DefaultCellStyle.Format = "N0";
                    if (dgvUsedServices.Columns["Thành tiền"] != null) dgvUsedServices.Columns["Thành tiền"].DefaultCellStyle.Format = "N0";
                }
            }
            else
            {
                dgvUsedServices.DataSource = null;
                lblCheckInTime.Text = "Giờ nhận: --";
            }
        }

        private void BtnAddService_Click(object sender, EventArgs e)
        {
            if (currentPhieu == null || cboServices.SelectedValue == null) return;

            int maDV = (int)cboServices.SelectedValue;
            int qty = (int)numServiceQty.Value;

            bus.AddServiceUsage(currentPhieu.MaPhieu, maDV, qty);
            dgvUsedServices.DataSource = bus.GetServiceDetails(currentPhieu.MaPhieu);
        }

        private decimal CalculateTotal()
        {
            if (currentPhieu == null) return 0;

            // Tính số giờ (Làm tròn lên ít nhất 1 giờ)
            TimeSpan duration = DateTime.Now - currentPhieu.NgayNhanPhong;
            int hours = (int)Math.Ceiling(duration.TotalHours);
            if (hours < 1) hours = 1;

            decimal roomCost = hours * currentHourlyPrice;

            decimal serviceCost = 0;
            if (dgvUsedServices.DataSource is DataTable dt)
            {
                foreach (DataRow r in dt.Rows)
                {
                    serviceCost += Convert.ToDecimal(r["Thành tiền"]);
                }
            }

            return roomCost + serviceCost;
        }

        private void BtnCalculate_Click(object sender, EventArgs e)
        {
            decimal total = CalculateTotal();
            lblTotalAmount.Text = $"Tổng tiền: {total:N0} VNĐ";
        }

        private void BtnCheckOut_Click(object sender, EventArgs e)
        {
            if (currentPhieu == null) return;

            decimal total = CalculateTotal();

            if (MessageBox.Show($"Xác nhận thanh toán và Check-out phòng {currentPhieu.SoPhong}?\nTổng tiền: {total:N0} VNĐ",
                                "Xác nhận Check-out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (bus.CheckOut(currentPhieu.MaPhieu, currentPhieu.SoPhong, total))
                {
                    MessageBox.Show("Thanh toán thành công! Trạng thái phòng đã chuyển sang 'Đang dọn dẹp'.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadDataTab1();
                    LoadDataTab2();
                    LoadDataTab3();
                }
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmHotelMain());
        }
    }
    #endregion
}