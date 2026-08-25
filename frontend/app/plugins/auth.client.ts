export default defineNuxtPlugin(async () => {
  const { accessToken, currentUser, fetchMe } = useAuth();

  if (accessToken.value && !currentUser.value) {
    await fetchMe();
  }
});
