<template>
  <div class="goods-audit-container">
    <div class="search-bar">
      <el-input 
        placeholder="搜索商品名称..." 
        v-model="searchKeyword" 
        @keyup.enter.native="handleSearch"
        clearable
        class="search-input">
        <el-button slot="append" icon="el-icon-search" @click="handleSearch"></el-button>
      </el-input>
    </div>

    <el-table
      :data="pendingGoods"
      stripe
      v-loading="loading"
      style="width: 100%"
      class="audit-table">
      <el-table-column type="index" label="序号" width="60"></el-table-column>
      <el-table-column prop="title" label="商品名称" show-overflow-tooltip min-width="150"></el-table-column>
      <el-table-column prop="description" label="商品描述" show-overflow-tooltip min-width="200"></el-table-column>
      <el-table-column prop="price" label="价格" width="100">
        <template slot-scope="scope">
          ¥{{ scope.row.price }}
        </template>
      </el-table-column>
      <el-table-column prop="categoryId" label="分类ID" width="100"></el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template slot-scope="scope">
          <el-button size="mini" type="success" @click="handleAudit(scope.row, 1)">通过</el-button>
          <el-button size="mini" type="danger" @click="handleAudit(scope.row, 2)">拒绝</el-button>
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
  name: "GoodsAudit",
  data() {
    return {
      loading: false,
      currentPage: 1,
      pageSize: 10,
      total: 0,
      searchKeyword: '',
      pendingGoods: []
    }
  },
  created() {
    this.loadPendingGoods();
  },
  methods: {
    // 加载待审核商品
    loadPendingGoods() {
      this.loading = true;
      this.$api.getPendingGoods({
        page: this.currentPage,
        pageSize: this.pageSize
      }).then(res => {
        this.loading = false;
        if (res.status_code === 1) {
          const data = res.data || {};
          this.pendingGoods = data.items || data.list || [];
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
    // 审核商品
    handleAudit(row, status) {
      const action = status === 1 ? '通过' : '拒绝';
      this.$confirm(`确认${action}该商品吗?`, '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.$api.auditGoods(row.id, { auditStatus: status }).then(res => {
          if (res.status_code === 1) {
            this.$message.success(`${action}成功`);
            this.loadPendingGoods();
          } else {
            this.$message.error(res.msg || `${action}失败`);
          }
        }).catch(e => {
          console.error(e);
          this.$message.error(`${action}失败，请检查网络`);
        });
      }).catch(() => {});
    },
    // 搜索
    handleSearch() {
      this.currentPage = 1;
      this.loadPendingGoods();
    },
    // 分页
    handlePageChange(page) {
      this.currentPage = page;
      this.loadPendingGoods();
    }
  }
}
</script>

<style scoped>
.goods-audit-container {
  padding: 20px;
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.search-bar {
  margin-bottom: 20px;
}

.search-input {
  width: 300px;
}

.audit-table {
  margin-bottom: 20px;
}

.pagination {
  display: flex;
  justify-content: center;
  padding-top: 20px;
}
</style>
