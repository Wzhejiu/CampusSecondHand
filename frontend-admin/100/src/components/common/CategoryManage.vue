<template>
  <div class="category-manage-container">
    <div class="header-bar">
      <el-button type="primary" icon="el-icon-plus" @click="handleAdd">添加分类</el-button>
    </div>

    <el-table
      :data="categories"
      stripe
      v-loading="loading"
      style="width: 100%"
      class="category-table">
      <el-table-column type="index" label="序号" width="60"></el-table-column>
      <el-table-column prop="categoryId" label="分类ID" width="80"></el-table-column>
      <el-table-column prop="categoryName" label="分类名称" min-width="150"></el-table-column>
      <el-table-column prop="parentId" label="父分类ID" width="120">
        <template slot-scope="scope">
          <span v-if="scope.row.parentId">{{ scope.row.parentId }}</span>
          <el-tag v-else type="info" size="small">顶级分类</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template slot-scope="scope">
          <el-button size="mini" type="primary" @click="handleEdit(scope.row)">编辑</el-button>
          <el-button size="mini" type="danger" @click="handleDelete(scope.row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 添加/编辑分类对话框 -->
    <el-dialog :title="dialogTitle" :visible.sync="dialogVisible" width="400px">
      <el-form :model="categoryForm" label-width="80px">
        <el-form-item label="分类名称">
          <el-input v-model="categoryForm.categoryName" placeholder="请输入分类名称"></el-input>
        </el-form-item>
        <el-form-item label="父分类">
          <el-select v-model="categoryForm.parentId" placeholder="请选择父分类(可选)" clearable style="width: 100%">
            <el-option 
              v-for="cat in topCategories" 
              :key="cat.categoryId || cat.id" 
              :label="cat.categoryName" 
              :value="cat.categoryId || cat.id">
            </el-option>
          </el-select>
        </el-form-item>
      </el-form>
      <span slot="footer" class="dialog-footer">
        <el-button @click="dialogVisible = false">取 消</el-button>
        <el-button type="primary" @click="submitCategory">确 定</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
export default {
  name: "CategoryManage",
  data() {
    return {
      loading: false,
      categories: [],
      dialogVisible: false,
      dialogTitle: '添加分类',
      editingId: null,
      categoryForm: {
        categoryName: '',
        parentId: null
      }
    }
  },
  computed: {
    topCategories() {
      return this.categories.filter(c => !c.parentId);
    }
  },
  created() {
    this.loadCategories();
  },
  methods: {
    // 加载分类列表
    loadCategories() {
      this.loading = true;
      this.$api.getCategories().then(res => {
        this.loading = false;
        if (res.status_code === 1) {
          const data = res.data || {};
          this.categories = data.items || data.list || data || [];
        } else {
          this.$message.error(res.msg || '加载失败');
        }
      }).catch(e => {
        this.loading = false;
        console.error(e);
        this.$message.error('加载失败，请检查网络');
      });
    },
    // 添加分类
    handleAdd() {
      this.dialogTitle = '添加分类';
      this.editingId = null;
      this.categoryForm = {
        categoryName: '',
        parentId: null
      };
      this.dialogVisible = true;
    },
    // 编辑分类
    handleEdit(row) {
      this.dialogTitle = '编辑分类';
      this.editingId = row.categoryId || row.id;
      this.categoryForm = {
        categoryName: row.categoryName,
        parentId: row.parentId
      };
      this.dialogVisible = true;
    },
    // 提交分类
    submitCategory() {
      if (!this.categoryForm.categoryName) {
        this.$message.warning('请输入分类名称');
        return;
      }

      if (this.editingId) {
        // 编辑
        this.$api.updateCategory(this.editingId, {
          categoryName: this.categoryForm.categoryName
        }).then(res => {
          if (res.status_code === 1) {
            this.$message.success('更新成功');
            this.dialogVisible = false;
            this.loadCategories();
          } else {
            this.$message.error(res.msg || '更新失败');
          }
        }).catch(e => {
          console.error(e);
          this.$message.error('更新失败，请检查网络');
        });
      } else {
        // 添加
        this.$api.createCategory(this.categoryForm).then(res => {
          if (res.status_code === 1) {
            this.$message.success('添加成功');
            this.dialogVisible = false;
            this.loadCategories();
          } else {
            this.$message.error(res.msg || '添加失败');
          }
        }).catch(e => {
          console.error(e);
          this.$message.error('添加失败，请检查网络');
        });
      }
    },
    // 删除分类
    handleDelete(row) {
      this.$confirm(`确认删除分类 "${row.categoryName}" 吗?`, '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.$api.deleteCategory(row.categoryId || row.id).then(res => {
          if (res.status_code === 1) {
            this.$message.success('删除成功');
            this.loadCategories();
          } else {
            this.$message.error(res.msg || '删除失败');
          }
        }).catch(e => {
          console.error(e);
          this.$message.error('删除失败，请检查网络');
        });
      }).catch(() => {});
    }
  }
}
</script>

<style scoped>
.category-manage-container {
  padding: 20px;
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.header-bar {
  margin-bottom: 20px;
  display: flex;
  justify-content: flex-end;
}

.category-table {
  margin-bottom: 20px;
}
</style>
