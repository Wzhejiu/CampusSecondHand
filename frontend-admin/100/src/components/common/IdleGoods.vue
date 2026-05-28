<template>
  <div class="goods-container">
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

    <el-tabs v-model="activeTab" @tab-click="handleTabClick" class="goods-tabs">
      <el-tab-pane label="已上架" name="online">
        <el-table
          :data="onlineGoods"
          stripe
          v-loading="loading"
          style="width: 100%">
          <el-table-column type="index" label="序号" width="60"></el-table-column>
          <el-table-column prop="title" label="商品名称" show-overflow-tooltip min-width="150"></el-table-column>
          <el-table-column prop="description" label="商品描述" show-overflow-tooltip min-width="200"></el-table-column>
          <el-table-column prop="price" label="价格" width="100">
            <template slot-scope="scope">
              ¥{{ scope.row.price }}
            </template>
          </el-table-column>
          <el-table-column prop="categoryId" label="分类ID" width="100"></el-table-column>
          <el-table-column label="操作" width="150" fixed="right">
            <template slot-scope="scope">
              <el-button size="mini" type="danger" @click="handleOffline(scope.row)">下架</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>

      <el-tab-pane label="已下架" name="offline">
        <el-table
          :data="offlineGoods"
          stripe
          v-loading="loading"
          style="width: 100%">
          <el-table-column type="index" label="序号" width="60"></el-table-column>
          <el-table-column prop="title" label="商品名称" show-overflow-tooltip min-width="150"></el-table-column>
          <el-table-column prop="description" label="商品描述" show-overflow-tooltip min-width="200"></el-table-column>
          <el-table-column prop="price" label="价格" width="100">
            <template slot-scope="scope">
              ¥{{ scope.row.price }}
            </template>
          </el-table-column>
          <el-table-column prop="categoryId" label="分类ID" width="100"></el-table-column>
          <el-table-column label="操作" width="150" fixed="right">
            <template slot-scope="scope">
              <el-button size="mini" type="danger" @click="handleDelete(scope.row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>
    </el-tabs>

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
    name: "IdleGoods",
    data() {
        return {
            loading: false,
            activeTab: 'online',
            currentPage: 1,
            pageSize: 10,
            total: 0,
            searchKeyword: '',
            onlineGoods: [],
            offlineGoods: []
        }
    },
    created() {
        this.loadGoods();
    },
    methods: {
        // 加载商品数据
        loadGoods() {
            this.loading = true;
            const params = {
                page: this.currentPage,
                pageSize: this.pageSize,
                keyword: this.searchKeyword || 'all'  // keyword是必填参数
            };

            // 根据api.txt: /api/Goods 的参数是 categoryId, keyword, page, pageSize
            // 已上架商品不需要额外参数
            // 已下架商品通过 categoryId 或其他方式筛选
            let apiPromise;

            if (this.activeTab === 'online') {
                apiPromise = this.$api.getGoods(params);
            } else if (this.activeTab === 'offline') {
                // 已下架商品使用 /api/Goods/my 接口，auditStatus=2 表示已下架
                apiPromise = this.$api.getMyGoods({
                    auditStatus: 2,
                    page: this.currentPage,
                    pageSize: this.pageSize
                });
            }

            apiPromise.then(res => {
                this.loading = false;
                if (res.status_code === 1) {
                    // 兼容不同的响应数据格式
                    const data = res.data || {};
                    const list = data.items || data.list || [];
                    this.total = data.total || data.count || 0;

                    switch(this.activeTab) {
                        case 'online':
                            this.onlineGoods = list;
                            break;
                        case 'offline':
                            this.offlineGoods = list;
                            break;
                    }
                } else {
                    this.$message.error(res.msg || '加载数据失败');
                }
            }).catch(e => {
                this.loading = false;
                console.error(e);
                this.$message.error('网络错误，请稍后重试');
            });
        },

        // 搜索商品
        handleSearch() {
            this.currentPage = 1;
            this.loading = true;
            // 根据api.txt: /api/Goods 支持 keyword 参数搜索
            this.$api.getGoods({
                keyword: this.searchKeyword || undefined,
                page: this.currentPage,
                pageSize: this.pageSize
            }).then(res => {
                this.loading = false;
                if (res.status_code === 1) {
                    const data = res.data || {};
                    const list = data.items || data.list || [];
                    this.total = data.total || data.count || 0;

                    switch(this.activeTab) {
                        case 'online':
                            this.onlineGoods = list;
                            break;
                        case 'offline':
                            this.offlineGoods = list;
                            break;
                    }
                } else {
                    this.$message.error(res.msg || '搜索失败');
                }
            }).catch(e => {
                this.loading = false;
                console.error(e);
                this.$message.error('搜索失败，请稍后重试');
            });
        },

        // 下架商品
        handleOffline(row) {
            this.$confirm('确定要下架该商品吗？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
                this.loading = true;
                this.$api.updateGoodsStatus(row.id, { auditStatus: 2 }).then(res => {
                    this.loading = false;
                    if (res.status_code === 1) {
                        this.$message.success('下架成功');
                        this.loadGoods();
                    } else {
                        this.$message.error(res.msg || '下架失败');
                    }
                }).catch(e => {
                    this.loading = false;
                    console.error(e);
                    this.$message.error('下架失败，请稍后重试');
                });
            }).catch(() => {});
        },

        // 删除商品
        handleDelete(row) {
            this.$confirm('确定要永久删除该商品吗？此操作不可恢复！', '警告', {
                confirmButtonText: '确定删除',
                cancelButtonText: '取消',
                type: 'error'
            }).then(() => {
                this.loading = true;
                this.$api.deleteGoods(row.id).then(res => {
                    this.loading = false;
                    if (res.status_code === 1) {
                        this.$message.success('删除成功');
                        this.loadGoods();
                    } else {
                        this.$message.error(res.msg || '删除失败');
                    }
                }).catch(e => {
                    this.loading = false;
                    console.error(e);
                    this.$message.error('删除失败，请稍后重试');
                });
            }).catch(() => {});
        },

        // 切换标签页
        handleTabClick() {
            this.currentPage = 1;
            this.searchKeyword = '';
            this.loadGoods();
        },

        // 分页变化
        handlePageChange(val) {
            this.currentPage = val;
            this.loadGoods();
        }
    }
}
</script>

<style scoped>
.goods-container {
  background-color: #fff;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.search-bar {
  margin-bottom: 20px;
}

.search-input {
  max-width: 400px;
}

.goods-tabs {
  margin-top: 20px;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: center;
}
</style>