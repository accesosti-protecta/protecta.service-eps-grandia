using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia.Util
{
    public static class DataReaderExtensions
    {
        public static List<T> ReadRowsList<T>(this IDataReader reader) where T : new()
        {
            System.Collections.Hashtable hashtable = GetProperties<T>();
            List<T> entities = new List<T>();

            while (reader.Read())
            {
                T newObject = reader.ReadFields<T>(hashtable);
                entities.Add(newObject);
            }
            return entities;
        }

        public static T ReadFields<T>(this IDataReader reader, System.Collections.Hashtable properties = null) where T : new()
        {
            T entity = new T();
            if (properties == null)
            {
                properties = GetProperties<T>();
            }

            System.Reflection.PropertyInfo info;

            for (int index = 0; index < reader.FieldCount; index++)
            {
                info = (System.Reflection.PropertyInfo)properties[reader.GetName(index)];
                if ((info != null) && info.CanWrite)
                {
                    try
                    {
                        info.SetValue(entity, reader.GetValue(index) == DBNull.Value ? default(T) : reader.GetValue(index), null);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(String.Format("Error de comptabilidad en la asignación del campo [Origen (Tipo) -> Destino (Tipo)] : {0}({1}) -> {2}({3})",
                                            reader.GetName(index).ToString(),
                                            reader.GetValue(index).GetType().Name,
                                            info.Name,
                                            info.PropertyType.Name
                                            ),
                                            e);

                    }
                }
            }
            return entity;
        }

        private static System.Collections.Hashtable GetProperties<T>() where T : new()
        {
            System.Collections.Hashtable properties = new System.Collections.Hashtable();
            Type businessEntityType = typeof(T);
            System.Reflection.PropertyInfo[] propertiesInfo = businessEntityType.GetProperties();
            foreach (System.Reflection.PropertyInfo property in propertiesInfo)
            {
                properties[property.Name] = property;
            }
            return properties;
        }
    }
}
