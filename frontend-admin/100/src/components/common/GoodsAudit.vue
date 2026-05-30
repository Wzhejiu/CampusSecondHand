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
      <el-table-column label="商品图片" width="140">
        <template slot-scope="scope">
          <div v-if="scope.row._images && scope.row._images.length > 0" class="image-cell">
            <img
              :src="getImageUrl(scope.row._images[0])"
              class="thumbnail"
              @click="openPreview(scope.row._images)"
              alt="商品图片" />
            <span v-if="scope.row._images.length > 1" class="image-count">+{{ scope.row._images.length - 1 }}</span>
          </div>
          <el-button
            v-else
            size="mini"
            type="text"
            icon="el-icon-picture"
            :loading="scope.row._loadingImg"
            @click="loadGoodsImages(scope.row)">
            查看图片
          </el-button>
        </template>
      </el-table-column>
      <el-table-column prop="categoryId" label="分类ID" width="100"></el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template slot-scope="scope">
          <el-button size="mini" type="success" @click="handleAudit(scope.row, 1)">通过</el-button>
          <el-button size="mini" type="danger" @click="handleDelete(scope.row)">删除</el-button>
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

    <!-- 图片预览弹窗 -->
    <el-dialog
      title="图片预览"
      :visible.sync="previewVisible"
      append-to-body
      width="700px"
      custom-class="image-preview-dialog">
      <div class="preview-container">
        <div class="preview-main">
          <img :src="getImageUrl(previewImages[previewIndex])" alt="预览图片" class="preview-image" />
        </div>
        <div class="preview-nav" v-if="previewImages.length > 1">
          <el-button
            icon="el-icon-arrow-left"
            circle
            :disabled="previewIndex <= 0"
            @click="previewIndex--">
          </el-button>
          <span class="preview-counter">{{ previewIndex + 1 }} / {{ previewImages.length }}</span>
          <el-button
            icon="el-icon-arrow-right"
            circle
            :disabled="previewIndex >= previewImages.length - 1"
            @click="previewIndex++">
          </el-button>
        </div>
        <div class="preview-thumbnails" v-if="previewImages.length > 1">
          <div
            v-for="(img, idx) in previewImages"
            :key="idx"
            class="thumbnail-item"
            :class="{ active: idx === previewIndex }"
            @click="previewIndex = idx">
            <img :src="getImageUrl(img)" alt="缩略图" />
          </div>
        </div>
      </div>
    </el-dialog>
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
      pendingGoods: [],
      previewVisible: false,
      previewImages: [],
      previewIndex: 0
    }
  },
  created() {
    this.loadPendingGoods();
  },
  methods: {
    // 打开图片预览
    openPreview(images) {
      this.previewImages = images || [];
      this.previewIndex = 0;
      this.previewVisible = true;
    },

    // 加载商品图片（从列表数据或详情API获取）
    loadGoodsImages(row) {
      // 先尝试从列表数据中直接获取图片数组
      const listImages = row.images || row.imageUrls || row.image_urls || [];
      if (Array.isArray(listImages) && listImages.length > 0) {
        this.$set(row, '_images', listImages);
        this.$set(row, '_loadingImg', false);
        return;
      }

      // 列表有coverImage字段，作为图片显示
      if (row.coverImage) {
        this.$set(row, '_images', [row.coverImage]);
        this.$set(row, '_loadingImg', false);
        return;
      }

      // 列表无图片数据，尝试通过详情API获取
      const goodsId = row.goodsId || row.id;
      if (!goodsId) {
        this.$set(row, '_images', []);
        this.$set(row, '_loadingImg', false);
        return;
      }

      this.$set(row, '_loadingImg', true);
      this.$api.getGoodsDetail(goodsId).then(res => {
        if (res.status_code === 1 && res.data) {
          const goods = res.data;
          const imgs = goods.images || goods.imageUrls || goods.image_urls || [];
          if (Array.isArray(imgs) && imgs.length > 0) {
            this.$set(row, '_images', imgs);
          } else {
            this.$set(row, '_images', []);
          }
        } else {
          this.$set(row, '_images', []);
        }
      }).catch(e => {
        console.error(e);
        this.$set(row, '_images', []);
      }).finally(() => {
        this.$set(row, '_loadingImg', false);
      });
    },
    // 获取完整图片URL
    getImageUrl(path) {
      if (!path) return '';
      if (path.startsWith('http://') || path.startsWith('https://')) return path;
      return `http://172.19.229.149:5000${path.startsWith('/') ? path : '/' + path}`;
    },

    // 加载待审核商品
    loadPendingGoods() {
      this.loading = true;
      const params = {
        page: this.currentPage,
        pageSize: this.pageSize
      };
      // 如果有搜索关键词，添加到参数中
      if (this.searchKeyword) {
        params.keyword = this.searchKeyword;
      }
      this.$api.getPendingGoods(params).then(res => {
        this.loading = false;
        if (res.status_code === 1) {
          const data = res.data || {};
          this.pendingGoods = data.items || data.list || [];
          this.total = data.total || data.count || 0;

          // 自动为每行加载图片数据
          this.pendingGoods.forEach(row => {
            this.$set(row, '_images', []);
            this.$set(row, '_loadingImg', false);
            this.loadGoodsImages(row);
          });

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
        this.$api.auditGoods(row.goodsId || row.id, { auditStatus: status }).then(res => {
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
    // 删除商品
    handleDelete(row) {
      this.$confirm('确认删除该商品吗?', '警告', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.$api.deleteGoods(row.goodsId || row.id).then(res => {
          if (res.status_code === 1) {
            this.$message.success('删除成功');
            this.loadPendingGoods();
          } else {
            this.$message.error(res.msg || '删除失败');
          }
        }).catch(e => {
          console.error(e);
          this.$message.error('删除失败，请检查网络');
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

.image-cell {
  position: relative;
  display: inline-block;
  cursor: pointer;
}

.thumbnail {
  width: 80px;
  height: 80px;
  border-radius: 4px;
  object-fit: cover;
  display: block;
}

.image-count {
  position: absolute;
  bottom: 4px;
  right: 4px;
  background: rgba(0, 0, 0, 0.6);
  color: #fff;
  font-size: 12px;
  padding: 1px 6px;
  border-radius: 10px;
}

.preview-container {
  text-align: center;
}

.preview-main {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 300px;
}

.preview-image {
  max-width: 100%;
  max-height: 450px;
  object-fit: contain;
  border-radius: 4px;
}

.preview-nav {
  display: flex;
  justify-content: center;
  align-items: center;
  margin-top: 16px;
  gap: 16px;
}

.preview-counter {
  font-size: 14px;
  color: #606266;
  min-width: 60px;
}

.preview-thumbnails {
  display: flex;
  justify-content: center;
  gap: 8px;
  margin-top: 16px;
  flex-wrap: wrap;
}

.thumbnail-item {
  width: 60px;
  height: 60px;
  border-radius: 4px;
  overflow: hidden;
  cursor: pointer;
  border: 2px solid transparent;
  transition: border-color 0.2s;
}

.thumbnail-item.active {
  border-color: #409EFF;
}

.thumbnail-item:hover {
  border-color: #409EFF;
}

.thumbnail-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
