<template>
  <div class="user-manage-container">
    <div class="search-bar">
      <el-input
        placeholder="搜索用户名..."
        v-model="searchKeyword"
        @keyup.enter.native="handleSearch"
        clearable
        class="search-input">
        <el-button slot="append" icon="el-icon-search" @click="handleSearch"></el-button>
      </el-input>
    </div>

    <el-table
      :data="users"
      stripe
      v-loading="loading"
      style="width: 100%"
      class="user-table">
      <el-table-column type="index" label="序号" width="60"></el-table-column>
      <el-table-column prop="userId" label="用户ID" width="80"></el-table-column>
      <el-table-column prop="username" label="用户名" show-overflow-tooltip min-width="120"></el-table-column>
      <el-table-column prop="phone" label="手机号" width="130"></el-table-column>
      <el-table-column label="角色" width="120">
        <template slot-scope="scope">
          <el-tag :type="getRoleType(Number(scope.row.role))">
            {{ getRoleText(Number(scope.row.role)) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="注册时间" width="180">
        <template slot-scope="scope">
          {{ scope.row.createTime || scope.row.create_time || '' }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template slot-scope="scope">
          <el-button
            v-if="Number(scope.row.role) === 0"
            size="mini"
            type="warning"
            @click="handleUpdateRole(scope.row, 1)">
            设为管理员
          </el-button>
          <el-button
            v-if="Number(scope.row.role) === 1"
            size="mini"
            type="info"
            @click="handleUpdateRole(scope.row, 0)">
            取消管理员
          </el-button>
          <el-button
            size="mini"
            type="danger"
            @click="handleDelete(scope.row)">
            删除
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
  name: "UserManage",
  data() {
    return {
      loading: false,
      currentPage: 1,
      pageSize: 10,
      total: 0,
      searchKeyword: '',
      users: []
    }
  },
  created() {
    this.loadUsers();
  },
  methods: {
    // 加载用户列表
    loadUsers() {
      this.loading = true;
      const params = {
        page: this.currentPage,
        pageSize: this.pageSize,
        keyword: this.searchKeyword || ''
      };

      this.$api.getUserList(params).then(res => {
        this.loading = false;
        if (res.status_code === 1) {
          const data = res.data || {};
          this.users = data.items || data.list || [];
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
    // 更新用户角色
    handleUpdateRole(user, role) {
      const actionText = this.getRoleText(role);
      this.$confirm(`确认将用户 "${user.username}" 设为${actionText}吗?`, '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        const userId = user.userId || user.id || user.user_id;
        this.$api.updateUserRole(Number(userId), { role: Number(role) }).then(res => {
          if (res.status_code === 1) {
            this.$message.success('操作成功');
            this.loadUsers();
          } else {
            this.$message.error(res.msg || '操作失败');
          }
        }).catch(e => {
          console.error(e);
          if (e.response && e.response.data) {
            const errData = e.response.data;
            const errMsg = errData.message || errData.msg || errData.title || '操作失败';
            this.$message.error(errMsg);
          } else {
            this.$message.error('操作失败，请检查网络');
          }
        });
      }).catch(() => {});
    },
    // 删除用户
    handleDelete(user) {
      const userId = user.userId || user.id || user.user_id;
      this.$confirm(`确认删除用户 "${user.username}" 吗？此操作不可恢复！`, '警告', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.$api.deleteUser(Number(userId)).then(res => {
          if (res.status_code === 1) {
            this.$message.success('删除成功');
            this.loadUsers();
          } else {
            this.$message.error(res.msg || '删除失败');
          }
        }).catch(e => {
          console.error(e);
          if (e.response && e.response.data) {
            const errData = e.response.data;
            const errMsg = errData.message || errData.msg || errData.title || '删除失败';
            this.$message.error(errMsg);
          } else {
            this.$message.error('删除失败，请检查网络');
          }
        });
      }).catch(() => {});
    },
    // 搜索
    handleSearch() {
      this.currentPage = 1;
      this.loadUsers();
    },
    // 分页
    handlePageChange(page) {
      this.currentPage = page;
      this.loadUsers();
    },
    // 获取角色文本
    getRoleText(role) {
      const roleMap = {
        0: '普通用户',
        1: '管理员',
        2: '已封号'
      };
      return roleMap[role] || '未知';
    },
    // 获取角色标签类型
    getRoleType(role) {
      const typeMap = {
        0: 'info',
        1: 'warning',
        2: 'danger'
      };
      return typeMap[role] || 'info';
    }
  }
}
</script>

<style scoped>
.user-manage-container {
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

.user-table {
  margin-bottom: 20px;
}

.pagination {
  display: flex;
  justify-content: center;
  padding-top: 20px;
}
</style>
