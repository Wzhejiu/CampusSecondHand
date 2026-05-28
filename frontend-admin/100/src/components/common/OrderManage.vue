<template>
  <div class="order-manage-container">
    <div class="search-bar">
      <el-input 
        placeholder="搜索订单..." 
        v-model="searchKeyword" 
        @keyup.enter.native="handleSearch"
        clearable
        class="search-input">
        <el-button slot="append" icon="el-icon-search" @click="handleSearch"></el-button>
      </el-input>
      <el-select v-model="statusFilter" placeholder="订单状态" clearable class="status-select" @change="handleSearch">
        <el-option label="待付款" :value="0"></el-option>
        <el-option label="待发货" :value="1"></el-option>
        <el-option label="待收货" :value="2"></el-option>
        <el-option label="已完成" :value="3"></el-option>
        <el-option label="已取消" :value="4"></el-option>
      </el-select>
    </div>

    <el-table
      :data="orders"
      stripe
      v-loading="loading"
      style="width: 100%"
      class="order-table">
      <el-table-column type="index" label="序号" width="60"></el-table-column>
      <el-table-column prop="id" label="订单ID" width="100"></el-table-column>
      <el-table-column prop="goodsId" label="商品ID" width="100"></el-table-column>
      <el-table-column prop="meetTime" label="交易时间" width="180"></el-table-column>
      <el-table-column prop="meetLocation" label="交易地点" show-overflow-tooltip min-width="150"></el-table-column>
      <el-table-column label="状态" width="100">
        <template slot-scope="scope">
          <el-tag :type="getStatusType(scope.row.status)">
            {{ getStatusText(scope.row.status) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template slot-scope="scope">
          <el-button 
            v-if="scope.row.status === 2"
            size="mini" 
            type="success" 
            @click="handleComplete(scope.row)">
            完成订单
          </el-button>
          <el-button 
            v-if="scope.row.status < 3"
            size="mini" 
            type="danger" 
            @click="handleCancel(scope.row)">
            取消订单
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <div class="pagination">
      <el-pagination
        @current-change="handlePageChange"
        :current-page.sync="currentPage"
        :page-size="pageSize"
        background
        layout="prev, pager, next, jumper"
        :total="total">
      </el-pagination>
    </div>
  </div>
</template>

<script>
export default {
  name: "OrderManage",
  data() {
    return {
      loading: false,
      currentPage: 1,
      pageSize: 10,
      total: 0,
      searchKeyword: '',
      statusFilter: null,
      orders: []
    }
  },
  created() {
    this.loadOrders();
  },
  methods: {
    // 加载订单列表
    loadOrders() {
      this.loading = true;
      const params = {
        page: this.currentPage,
        pageSize: this.pageSize
      };

      if (this.statusFilter !== null) {
        params.status = this.statusFilter;
      }

      this.$api.getOrderList(params).then(res => {
        this.loading = false;
        if (res.status_code === 1) {
          const data = res.data || {};
          this.orders = data.items || data.list || [];
          this.total = data.total || data.count || 0;
        } else {
          this.$message.error(res.msg || '加载失败');
        }
      }).catch(e => {
        this.loading = false;
        console.error(e);
        this.$message.error('加载失败，请检查网络');
      });
    },
    // 完成订单
    handleComplete(row) {
      this.$confirm('确认完成该订单吗?', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.$api.completeOrder(row.id).then(res => {
          if (res.status_code === 1) {
            this.$message.success('订单已完成');
            this.loadOrders();
          } else {
            this.$message.error(res.msg || '操作失败');
          }
        }).catch(e => {
          console.error(e);
          this.$message.error('操作失败，请检查网络');
        });
      }).catch(() => {});
    },
    // 取消订单
    handleCancel(row) {
      this.$confirm('确认取消该订单吗?', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.$api.cancelOrder(row.id).then(res => {
          if (res.status_code === 1) {
            this.$message.success('订单已取消');
            this.loadOrders();
          } else {
            this.$message.error(res.msg || '操作失败');
          }
        }).catch(e => {
          console.error(e);
          this.$message.error('操作失败，请检查网络');
        });
      }).catch(() => {});
    },
    // 搜索
    handleSearch() {
      this.currentPage = 1;
      this.loadOrders();
    },
    // 分页
    handlePageChange(page) {
      this.currentPage = page;
      this.loadOrders();
    },
    // 获取状态文本
    getStatusText(status) {
      const statusMap = {
        0: '待付款',
        1: '待发货',
        2: '待收货',
        3: '已完成',
        4: '已取消'
      };
      return statusMap[status] || '未知';
    },
    // 获取状态标签类型
    getStatusType(status) {
      const typeMap = {
        0: 'info',
        1: 'warning',
        2: 'primary',
        3: 'success',
        4: 'danger'
      };
      return typeMap[status] || 'info';
    }
  }
}
</script>

<style scoped>
.order-manage-container {
  padding: 20px;
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.search-bar {
  margin-bottom: 20px;
  display: flex;
  gap: 15px;
}

.search-input {
  width: 300px;
}

.status-select {
  width: 150px;
}

.order-table {
  margin-bottom: 20px;
}

.pagination {
  display: flex;
  justify-content: center;
  padding-top: 20px;
}
</style>
