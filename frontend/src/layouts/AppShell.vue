<script setup lang="ts">
import { RouterView } from 'vue-router';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import AppTopbar from '@/components/layout/AppTopbar.vue';
import MobileNav from '@/components/layout/MobileNav.vue';
</script>

<template>
  <div class="flex min-h-screen bg-bg text-text">
    <AppSidebar />

    <div class="flex min-w-0 flex-1 flex-col">
      <AppTopbar />
      <main class="scroll-slim w-full max-w-[1600px] flex-1 px-4 py-6 pb-28 md:px-8 md:pb-8">
        <RouterView />
      </main>
    </div>

    <MobileNav />
  </div>
</template>
