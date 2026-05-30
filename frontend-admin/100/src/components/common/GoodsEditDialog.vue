<template>
  <el-dialog
    :title="dialogTitle"
    :visible.sync="dialogVisible"
    width="600px"
    @close="handleClose">
    <el-form :model="formData" :rules="rules" ref="goodsForm" label-width="100px">
      <el-form-item label="商品名称" prop="title">
        <el-input v-model="formData.title" placeholder="请输入商品名称" maxlength="100"></el-input>
      </el-form-item>

      <el-form-item label="商品描述" prop="description">
        <el-input
          type="textarea"
          v-model="formData.description"
          placeholder="请输入商品描述"
          :rows="4"
          maxlength="500"></el-input>
      </el-form-item>

      <el-form-item label="价格" prop="price">
        <el-input-number
          v-model="formData.price"
          :precision="2"
          :step="0.01"
          :min="0"
          :max="999999"
          style="width: 100%"></el-input-number>
      </el-form-item>

      <el-form-item label="商品分类" prop="categoryId">
        <el-select 
          v-model="formData.categoryId" 
          placeholder="请选择分类" 
          clearable
          filterable
          style="width: 100%">
          <el-option
            v-for="item in categories"
            :key="item.id"
            :label="item.categoryName"
            :value="item.id"
            :disabled="!item.id"></el-option>
        </el-select>
      </el-form-item>

      <el-form-item label="商品图片" prop="images">
        <el-upload
          action="#" 
          list-type="picture-card" 
          :file-list="imageList" 
          :on-preview="handlePicturePreview" 
          :on-remove="handleRemove" 
          :on-change="handleChange" 
          :http-request="handleUpload" 
          :limit="5" 
          :on-exceed="handleExceed" 
          :auto-upload="true">
          <i class="el-icon-plus"></i>
        </el-upload>
        <el-dialog :visible.sync="previewVisible" append-to-body>
          <img width="100%" :src="previewImage" alt="预览图片">
        </el-dialog>
      </el-form-item>
    </el-form>

    <div slot="footer" class="dialog-footer">
      <el-button @click="dialogVisible = false">取 消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitLoading">确 定</el-button>
    </div>
  </el-dialog>
</template>

<script>
export default {
  name: 'GoodsEditDialog',
  props: {
    visible: {
      type: Boolean,
      default: false
    },
    goodsData: {
      type: Object,
      default: () => ({})
    }
  },
  data() {
    return {
      dialogVisible: false,
      submitLoading: false,
      previewVisible: false,
      previewImage: '',
      categories: [],
      imageList: [],
      formData: {
        id: null,
        title: '',
        description: '',
        price: 0,
        categoryId: null,
        images: []
      },
      rules: {
        title: [
          { required: true, message: '请输入商品名称', trigger: 'blur' },
          { min: 2, max: 100, message: '长度在 2 到 100 个字符', trigger: 'blur' }
        ],
        description: [
          { required: true, message: '请输入商品描述', trigger: 'blur' }
        ],
        price: [
          { required: true, message: '请输入商品价格', trigger: 'blur' }
        ],
        categoryId: [
          { required: true, message: '请选择商品分类', trigger: 'change' }
        ]
      }
    };
  },
  computed: {
    dialogTitle() {
      return this.formData.id ? '编辑商品' : '新增商品';
    }
  },
  watch: {
    visible(val) {
      this.dialogVisible = val;
      if (val) {
        this.initData();
      }
    }
  },
  methods: {
    // 初始化数据
    async initData() {
      await this.loadCategories();

      if (this.goodsData && (this.goodsData.id || this.goodsData.goodsId)) {
        // 编辑模式，从API加载商品详情
        try {
          const goodsId = this.goodsData.goodsId || this.goodsData.id;
          const res = await this.$api.getGoodsDetail(goodsId);
          if (res.status_code === 1 && res.data) {
            const goods = res.data;
            console.log('商品详情:', goods);
            console.log('images字段:', goods.images, 'imageUrls字段:', goods.imageUrls);
            this.formData = {
              id: goods.goodsId || goods.id,
              title: goods.title || '',
              description: goods.description || '',
              price: parseFloat(goods.price) || 0,
              categoryId: goods.categoryId || goods.category_id || null,
              images: goods.images || goods.imageUrls || []
            };

            // 处理图片列表，编辑模式下直接使用服务器返回的URL
            // 拼接完整的图片URL
            const baseUrl = 'http://172.19.229.149:5000';
            this.imageList = this.formData.images.map((url, index) => {
              // 确保URL是字符串类型
              const urlStr = String(url || '');
              // 如果URL不是完整的http地址，则拼接基础URL
              const fullUrl = urlStr.startsWith('http') ? urlStr : `${baseUrl}${urlStr}`;
              return {
                name: `image_${index}`,
                url: fullUrl,
                status: 'success' // 标记为已成功上传
              };
            });
          } else {
            this.$message.error(res.msg || '加载商品详情失败');
          }
        } catch (e) {
          console.error(e);
          this.$message.error('加载商品详情失败');
        }
      } else {
        // 新增模式，重置表单
        this.resetForm();
      }
    },

    // 加载分类列表
    loadCategories() {
      return this.$api.getCategories().then(res => {
        if (res.status_code === 1) {
          const data = res.data || {};
          // 处理分类数据，兼容items、list或直接返回数组的情况
          const categories = data.items || data.list || data || [];
          // 统一处理分类数据，确保有id和categoryName字段
          this.categories = categories.map(cat => ({
            id: cat.categoryId || cat.id,
            categoryName: cat.categoryName,
            parentId: cat.parentId
          }));
        } else {
          this.$message.error(res.msg || '加载分类失败');
        }
      }).catch(e => {
        console.error(e);
        this.$message.error('加载分类失败');
      });
    },

    // 重置表单
    resetForm() {
      // 释放所有本地预览URL
      this.imageList.forEach(file => {
        if (file.previewUrl) {
          URL.revokeObjectURL(file.previewUrl);
        }
      });
      
      // 先清除验证，再重置数据，避免重置时触发验证错误
      if (this.$refs.goodsForm) {
        this.$refs.goodsForm.clearValidate();
      }

      this.formData = {
        id: null,
        title: '',
        description: '',
        price: 0,
        categoryId: null,
        images: []
      };
      this.imageList = [];
    },

    // 处理图片上传
    handleUpload(options) {
      const { file, onSuccess, onError } = options;
      this.$api.uploadFile(file).then(res => {
        if (res.status_code === 1) {
          // 解析上传API返回的图片URL
          // res.data可能是字符串或对象，需要正确处理
          let imageUrl = '';
          if (typeof res.data === 'string') {
            imageUrl = res.data;
          } else if (res.data && typeof res.data === 'object') {
            imageUrl = res.data.url || res.data.image_url || res.data.imageUrl || res.data.path || '';
          } else if (res.url) {
            imageUrl = typeof res.url === 'string' ? res.url : (res.url.url || '');
          }
          
          console.log('上传响应 res:', res);
          console.log('解析出的 imageUrl:', imageUrl);
          
          // 确保imageUrl是字符串类型
          const urlStr = String(imageUrl || '');
          
          // 拼接完整的图片URL
          const baseUrl = 'http://172.19.229.149:5000';
          const fullUrl = urlStr.startsWith('http') ? urlStr : `${baseUrl}${urlStr}`;
          
          // 保存上传后的URL到文件对象中
          // 使用onSuccess通知el-upload上传完成，并将URL保存到response中
          onSuccess({ url: fullUrl, imageUrl: urlStr });
        } else {
          this.$message.error(res.msg || '上传失败');
          onError(res);
        }
      }).catch(e => {
        console.error(e);
        this.$message.error('上传失败');
        onError(e);
      });
    },

    // 处理文件变化
    handleChange(file, fileList) {
      // 直接使用el-upload提供的fileList
      console.log('handleChange file:', file.status, file.url, file.response);
      this.imageList = fileList;
    },

    // 处理图片移除
    handleRemove(file, fileList) {
      this.imageList = fileList;
    },

    // 处理图片预览
    handlePicturePreview(file) {
      this.previewImage = file.url;
      this.previewVisible = true;
    },

    // 处理超出上传限制
    handleExceed() {
      this.$message.warning('最多只能上传5张图片');
    },

    // 提交表单
    async handleSubmit() {
      this.$refs.goodsForm.validate(async (valid) => {
        if (valid) {
          this.submitLoading = true;

          try {
            // 检查是否有未上传完成的图片
            const uploadingFiles = this.imageList.filter(file => 
              file.status === 'uploading'
            );

            if (uploadingFiles.length > 0) {
              this.$message.warning('请等待所有图片上传完成');
              this.submitLoading = false;
              return;
            }

            // 收集所有已成功上传的图片URL
            // 从response中获取上传后的URL，如果没有response则从url中提取相对路径
            const images = this.imageList
              .filter(file => file.status === 'success')
              .map(file => {
                // 优先从response中获取相对路径
                if (file.response && file.response.imageUrl) {
                  return file.response.imageUrl;
                }
                // 否则从url中提取相对路径
                const urlStr = String(file.url || '');
                const baseUrl = 'http://172.19.229.149:5000';
                if (urlStr.startsWith(baseUrl)) {
                  return urlStr.substring(baseUrl.length);
                }
                if (urlStr.startsWith('blob:')) {
                  return ''; // 排除本地预览URL
                }
                return urlStr;
              })
              .filter(url => url); // 过滤空字符串

            const data = {
              title: this.formData.title,
              description: this.formData.description,
              price: this.formData.price,
              categoryId: this.formData.categoryId,
              images: images
            };
            console.log('提交数据:', data);
            console.log('imageUrls:', JSON.stringify(images));
            console.log('imageList:', this.imageList.map(f => ({ status: f.status, url: f.url, response: f.response })));

            this.$api.adminUpdateGoods(this.formData.id, data).then(res => {
              console.log('更新商品响应:', res);
              this.submitLoading = false;
              if (res.status_code === 1) {
                this.$message.success('保存成功');
                this.dialogVisible = false;
                this.$emit('success');
              } else {
                this.$message.error(res.msg || '保存失败');
              }
            }).catch(e => {
              this.submitLoading = false;
              console.error(e);
              this.$message.error('保存失败，请稍后重试');
            });
          } catch (e) {
            this.submitLoading = false;
            this.$message.error(e.message || '操作失败');
          }
        }
      });
    },

    // 关闭对话框
    handleClose() {
      this.resetForm();
      this.$emit('update:visible', false);
    }
  }
};
</script>

<style scoped>
.dialog-footer {
  text-align: right;
}
</style>
