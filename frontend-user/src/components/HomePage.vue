<template>
  <div>
    <h2>🏷️ 二手好物</h2>

    <div class="filter-bar">
      <input type="text" v-model="searchKeyword" placeholder="搜索商品" @input="loadGoods">
      <select v-model="selectedCategory" @change="loadGoods">
        <option value="">全部分类</option>
        <option value="教材教辅">📚 教材教辅</option>
        <option value="数码电子">📱 数码电子</option>
        <option value="生活用品">🧴 生活用品</option>
      </select>
    </div>

    <div v-if="goodsLoading" class="loading">加载中...</div>
    <div v-else-if="goodsList.length === 0" class="empty-state">✨ 暂无商品，点击右下角➕发布</div>
    <div v-else>
      <div v-for="goods in goodsList" :key="goods.id" class="goods-card" @click="showDetail(goods.id)">
        <div class="goods-title">{{ goods.name }}</div>
        <div class="goods-price">¥ {{ formatPrice(goods.price) }}</div>
        <div class="goods-meta">🏷️ {{ goods.category || '其他' }}</div>
      </div>
    </div>

    <div class="fab-btn" @click="openPublishModal">+</div>

    <div class="modal-overlay" id="publishModal" @click.self="closePublishModal">
      <div class="modal-content">
        <h3>✨ 发布闲置</h3>
        <input type="text" v-model="publishForm.name" placeholder="商品名称">
        <input type="number" v-model="publishForm.price" placeholder="价格" step="0.01">
        <select v-model="publishForm.category">
          <option value="教材教辅">教材教辅</option>
          <option value="数码电子">数码电子</option>
          <option value="生活用品">生活用品</option>
        </select>
        <button class="btn-primary" @click="publishGoods">发布商品</button>
        <button class="btn-outline" @click="closePublishModal">取消</button>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import { goodsAPI, getToken } from '../api'

export default {
  name: 'HomePage',
  setup() {
    const goodsList = ref([])
    const goodsLoading = ref(false)
    const searchKeyword = ref("")
    const selectedCategory = ref("")
    const publishForm = ref({ name: "", price: "", category: "教材教辅" })

    const formatPrice = (price) => parseFloat(price).toFixed(2)

    const loadGoods = async () => {
      goodsLoading.value = true
      const res = await goodsAPI.getList({
        search: searchKeyword.value,
        category: selectedCategory.value
      })
      if (res.success && res.data) {
        goodsList.value = res.data
      } else {
        goodsList.value = []
      }
      goodsLoading.value = false
    }

    const showDetail = (id) => {
      alert(`查看商品详情 ID: ${id}`)
    }

    const publishGoods = async () => {
      const { name, price, category } = publishForm.value
      if (!name) {
        alert("请输入商品名称")
        return
      }
      if (!price || parseFloat(price) <= 0) {
        alert("请输入有效价格")
        return
      }

      const res = await goodsAPI.publish({
        name,
        price: parseFloat(price),
        category
      })

      alert(res.message)
      if (res.success) {
        closePublishModal()
        await loadGoods()
        publishForm.value = { name: "", price: "", category: "教材教辅" }
      }
    }

    const openPublishModal = () => {
      const modal = document.getElementById("publishModal")
      if (modal) modal.style.display = "flex"
    }

    const closePublishModal = () => {
      const modal = document.getElementById("publishModal")
      if (modal) modal.style.display = "none"
    }

    onMounted(() => {
      const token = getToken()
      const user = localStorage.getItem("user")
      if (!token || !user) {
        window.location.href = '/login'
      }
      loadGoods()
    })

    return {
      goodsList,
      goodsLoading,
      searchKeyword,
      selectedCategory,
      publishForm,
      formatPrice,
      loadGoods,
      showDetail,
      publishGoods,
      openPublishModal,
      closePublishModal
    }
  }
}
</script>

<style scoped>
.filter-bar {
  display: flex;
  gap: 12px;
  margin: 16px 0 20px;
  flex-wrap: wrap;
}

.filter-bar input {
  flex: 1;
  margin: 0;
  min-width: 140px;
}

.filter-bar select {
  width: auto;
  margin: 0;
}

.goods-card {
  background: white;
  border-radius: 18px;
  padding: 16px;
  margin-bottom: 12px;
  border: 1px solid #edf2f7;
  cursor: pointer;
  transition: all 0.2s;
}

.goods-card:hover {
  border-color: #3b82f6;
  transform: translateY(-2px);
}

.goods-title {
  font-weight: 600;
  font-size: 1.05rem;
  margin-bottom: 6px;
  color: #0f172a;
}

.goods-price {
  color: #f97316;
  font-weight: 700;
  font-size: 1.2rem;
}

.goods-meta {
  font-size: 0.8rem;
  color: #64748b;
  margin-top: 6px;
}

.loading,
.empty-state {
  text-align: center;
  padding: 40px;
  color: #94a3b8;
}

.fab-btn {
  position: fixed;
  bottom: 80px;
  right: 24px;
  width: 56px;
  height: 56px;
  border-radius: 28px;
  background: #3b82f6;
  color: white;
  font-size: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 20px rgba(59, 130, 246, 0.4);
  cursor: pointer;
  z-index: 99;
}

.fab-btn:active {
  transform: scale(0.92);
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(3px);
  display: none;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 28px;
  width: 88%;
  max-width: 380px;
  padding: 24px;
}

.modal-content input,
.modal-content select {
  margin: 8px 0;
}

.btn-primary {
  background: #3b82f6;
  color: white;
  width: 100%;
  margin-top: 12px;
}

.btn-outline {
  background: white;
  border: 1px solid #cbd5e1;
  width: 100%;
  margin-top: 8px;
}
</style>